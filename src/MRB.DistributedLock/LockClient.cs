using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MRB.DistributedLock
{
    
        
    public class LockClient : ILockClient
    {
        private ILockRepostory _repo;
        private ILogger<LockClient> _logger;

        public LockClient(ILockRepostory repo, ILogger<LockClient> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ILockToken> WaitForLockAsync(Type ObjectType, Guid id, int waitTime = 60, CancellationToken? cancellationToken = null)
        {
            CancellationToken token = cancellationToken ?? CancellationToken.None;
            var startTime = DateTime.UtcNow;

            while (startTime < startTime.AddSeconds(waitTime))
            {
                //try to get a lock
                var lockToken = await TryAcquireLockAsync(ObjectType, id, token);
                if (lockToken != null)
                    return lockToken; // got a lock return it

                //no lock try again
                _logger.LogTrace("Waiting for token for {type} {id}", ObjectType, id);
                
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException();

                //limit the number of retries, I need to migrate this to polly
                Thread.Sleep((waitTime * 1000) / 10);
            }

            //No lock acquired so throw the error
            var msg = $"Timed out waiting for lock on {ObjectType} {id}";
            _logger.LogTrace(msg);
            throw new LockWaitTimeoutException(msg);
        }

        public async Task<ILockToken?> TryAcquireLockAsync(Type instanceType, Guid id, CancellationToken? cancellationToken = null)
        {
            CancellationToken token = cancellationToken ?? CancellationToken.None;

            try
            {
                
                //check if the lock already exists
                ILockToken? existingToken = await _repo.TryGetTokenAsync(instanceType, id);

                if (existingToken != null)
                {
                    // This should not happen much in practice
                    // Hitting this means that the owner of the lock 
                    // did not clean up the lock. It either crashed or was
                    // not disposed. So we will clean it up
                    if (existingToken.IsExpired())
                    {
                        
                        if (!await _repo.TryDeleteTokenAsync(existingToken))
                            //we could not delete the token, this 
                            //means that its still in use
                            //this happens if we load the lock while its
                            //being refreshed
                            return null;

                        existingToken = null;
                        
                    }
                    else
                        //The lock exists cant have a new lock
                        return null;
                }

                //no existing token, create a new one
                if (existingToken == null)
                {
                    OwnerLockToken newToken = new OwnerLockToken(instanceType, id, _repo);
                    var created = await _repo.TryCreateTokenAsync(newToken);
                    if (created)
                    {
                        _logger.LogInformation("Acquired Token for {type} {id}", instanceType.Name, id);
                        newToken.StartHeartBeat();
                        return newToken;
                    }
                    else
                    {
                        //Kill the token, we failed to create it
                        newToken.Dispose();
                    }
                }
                
                //If we get here it means that 
                //another process acquired the lock before
                //we could create it. We lost the race...
                return null;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Acquiring Token for {type}, {id}", instanceType.Name, id);
                throw;
            }
        }
    }
}