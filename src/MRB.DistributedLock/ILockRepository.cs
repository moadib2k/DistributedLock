using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRB.DistributedLock
{
    /// <summary>
    /// Interface for managing storage of <see cref="ILockToken"/> instances
    /// </summary>
    public interface ILockRepostory
    {
        /// <summary>
        /// Loads a lock for the specified <paramref name="insataneType"/> with the specified <paramref name="instanceId"/>
        /// </summary>
        /// <param name="insataneType">The <see cref="Type"/> of the instance to load the lock for</param>
        /// <param name="instanceId">The unique identifier of the instance</param>
        /// <returns>A <see cref="ILockToken"/> instance if the lock exists otherwise null</returns>
        Task<ILockToken?> TryGetTokenAsync(Type insataneType, Guid instanceId);

        /// <summary>
        /// Attempts to create a token in the repository
        /// </summary>
        /// <param name="token">The token to create</param>
        /// <returns>true if the lock could be created, false if a matching token already exists in the store</returns>
        Task<bool> TryCreateTokenAsync(ILockToken token);

        /// <summary>
        /// Attempts to delete a token from the repostory
        /// </summary>
        /// <param name="token">The token to delete</param>
        /// <returns>true if the final state is the token not being in the store, otherwise false</returns>
        /// <remarks>
        /// <para>If the token to be deleted from the store is not in the store this method will return true</para>
        /// </remarks>
        Task<bool> TryDeleteTokenAsync(ILockToken token);

        /// <summary>
        /// Tries to update a token to a new state
        /// </summary>
        /// <param name="token">The token to delete</param>
        /// <returns>tuple with success = True if the token could be updated otherwise success=false. The version 
        /// will be set to the new version number if the udpate was successful.</returns>
        Task<(bool success, uint version)> TryUpdateTokenAsync(ILockToken token);

    }
}
