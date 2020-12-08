using System;

namespace DistLockNet.Models
{
    public class LockingObject
    {
        public Guid Id { get; set; }
        public Guid LockId { get; set; }
        public string InstanceId { get; set; }
    }
}