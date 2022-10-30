using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRB.DistributedLock
{

    /// <summary>
    /// Interfarce for locks
    /// </summary>
    public interface ILockToken : IDisposable
    {
        /// <summary>
        /// The type of the item being locked
        /// </summary>
        string InstanceType { get; }

        /// <summary>
        /// The id of the item being locked
        /// </summary>
        Guid InstanceId { get; }

        /// <summary>
        /// The unique identifier for the lock
        /// </summary>
        Guid LockId { get; }

        /// <summary>
        /// Returns the <see cref="DateTime"/> of lock expiration
        /// </summary>
        DateTime Expires { get; }

        /// <summary>
        /// Counter indicating the number of times the lock has been refreshed
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Checks if the lock is expired
        /// </summary>
        /// <returns>true if the <see cref="Expires"/> is in the past otherwise false</returns>
        bool IsExpired();

    }

}
