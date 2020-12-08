using System;

namespace DistLockNet.Models
{
    public class LockingObject
    {
        public string AppId { get; set; }
        public Guid LockerId { get; set; }
        public Guid Seed { get; set; }
    }
}