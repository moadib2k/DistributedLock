using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRB.DistributedLock
{
    /// <summary>
    /// Interface for global sync locks
    /// </summary>
    public interface ILockClient
    {
        /// <summary>
        /// Acquires a lock for a resource
        /// </summary>
        /// <param name="ObjectType">The type of resource to lock</param>
        /// <param name="id">The uniqueidentifier of the resource</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the acquisition of the lock</param>
        /// <returns></returns>
        public Task<ILockToken?> TryAcquireLockAsync(Type ObjectType, Guid id, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Waits for a lock to be acquired 
        /// </summary>
        /// <param name="ObjectType">The type of resource to lock</param>
        /// <param name="id">The uniqueidentifier of the resource</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the acquisition of the lock</param>
        /// <param name="waitTime">The number of seconds to wait for a lock</param>
        /// <returns></returns>
        public Task<ILockToken> WaitForLockAsync(Type ObjectType, Guid id, int waitTime = 60, CancellationToken? cancellationToken = null);

    }
}
