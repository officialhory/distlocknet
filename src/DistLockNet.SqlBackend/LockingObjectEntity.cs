using System;
using System.ComponentModel.DataAnnotations;

namespace DistLockNet.SqlBackend
{
    public class LockingObjectEntity
    {
        [Key]
        public string AppId { get; set; }
        public Guid LockerId { get; set; }
        public Guid Seed { get; set; }
    }
}