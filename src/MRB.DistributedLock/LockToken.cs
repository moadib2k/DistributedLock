using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRB.DistributedLock
{

    public class LockToken : ILockToken
    {
        const uint LOCK_DURATION = 30;

        internal LockToken(string instanceType, Guid instanceId)
        {
            this.InstanceType = instanceType;
            this.InstanceId = instanceId;
            this.LockId = Guid.NewGuid();
        }

        public LockToken(Type instanceType, Guid instanceId) : this(instanceType.Name, instanceId)
        {
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="copyFrom"></param>
        internal LockToken(LockToken copyFrom)
        {
            this.InstanceType = copyFrom.InstanceType;
            this.InstanceId = copyFrom.InstanceId;
            this.LockId = copyFrom.LockId;
            this.Expires = copyFrom.Expires;
            this.Version = copyFrom.Version;
        }

        public string InstanceType { get; protected set; }
        public Guid InstanceId { get; protected set; }
        public Guid LockId { get; protected set; }
        public DateTime Expires { get; internal set; }
        public uint Version { get; internal set; }

        internal static Dictionary<string, AttributeValue> ToItem(ILockToken lockItem)
        {
            return new Dictionary<string, AttributeValue> {
                    { "InstanceType", new AttributeValue{ S = lockItem.InstanceType } },
                    { "InstanceId", new AttributeValue{S= lockItem.InstanceId.ToString() } },
                    { "LockId", new AttributeValue{S=lockItem.LockId.ToString() } },
                    { "Expires", new AttributeValue{S=lockItem.Expires.ToString("o")} },
                    { "Version", new AttributeValue{N=lockItem.Version.ToString()} }
            };
        }

        internal static readonly List<String> Attributes = new List<string> { "InstanceType", "InstanceId", "LockId", "Expires", "Version" };

        internal static LockToken FromItem(Dictionary<string, AttributeValue> item)
        {
            string typeName = item["InstanceType"].S;
            var id = Guid.Parse(item["InstanceId"].S);
            var owner = Guid.Parse(item["LockId"].S);
            var expires = DateTime.ParseExact(item["Expires"].S, "o", null, DateTimeStyles.AdjustToUniversal);
            var version = uint.Parse(item["Version"].N);

            return new LockToken(typeName, id) { LockId = owner, Expires = expires, Version = version };
        }

        protected virtual void Dispose(bool disposing)
        { }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > Expires;
        }
    }

}