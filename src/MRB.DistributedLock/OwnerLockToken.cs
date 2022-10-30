using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRB.DistributedLock
{

    public class OwnerLockToken : LockToken
    {
        private const int HEARTBEAT_FREQUENCY = 3*1000; //3 seconds
        private bool _disposed;
        private Timer? _heartbeatTimer;
        private SemaphoreSlim _sync = new SemaphoreSlim(1, 1);
        private ILockRepostory _repository;
        internal OwnerLockToken(Type instanceType, Guid instanceId, ILockRepostory repository) : base(instanceType, instanceId)
        {

            LockId = Guid.NewGuid();
            Expires = DateTime.UtcNow.AddSeconds(30);
            Version = 1;

            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
   
        }

        internal void StartHeartBeat()
        {
            _heartbeatTimer = new Timer(this.CheckState, null, HEARTBEAT_FREQUENCY, Timeout.Infinite);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_heartbeatTimer != null)
                {
                    //stop the heartbeat and kill the timer
                    _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    try
                    {
                        ReleaseTokenAsync().Wait();
                    }
                    catch (Exception)
                    {
                        //not much we can do, we are disposed and
                        //hit an error releasing the token
                        //It will expire on its own...
                    }
                    _heartbeatTimer.Dispose();
                }
            }

            _disposed = true;
        }


        private void CheckState(object? state)
        {
            if (_disposed)
                //nothing to do, we are already disposed
                return;

            try
            {
                RefreshAsync().Wait();
            }
            catch (Exception)
            {
                //swalllow the error
                //if its a transient error we will
                //catch it on the next heartbeat
            }
        }
        private async Task RefreshAsync()
        {
            if (_disposed)
                //nothing to do, we are already disposed
                return;

            try
            {
                _sync.Wait();

                LockToken token = new LockToken(this);
                token.Expires = DateTime.UtcNow.AddSeconds(30);
                var updated = await _repository.TryUpdateTokenAsync(token);

                if (updated.success)
                {
                    this.Expires = token.Expires;
                    this.Version = updated.version;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _heartbeatTimer?.Change(HEARTBEAT_FREQUENCY, Timeout.Infinite);
                _sync.Release();
            }

        }

        private async Task ReleaseTokenAsync()
        {
            if (_disposed)
                //nothing to do, we are already disposed
                return;

            try
            {
                _sync.Wait();

                var deleted = await _repository.TryDeleteTokenAsync(this);
                
                if (!deleted)
                {
                    //not much we can do here. We cannot delete the token
                }

                System.Diagnostics.Debug.WriteLine($"Released token for {this.InstanceType} {this.InstanceId}");

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _sync.Release();
            }
        }

    }
}
