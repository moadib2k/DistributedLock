using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRB.DistributedLock
{
    public class LockWaitTimeoutException : Exception
    {
        public LockWaitTimeoutException():base(){}
        public LockWaitTimeoutException(string? message) : base(message) { }
    }
}
