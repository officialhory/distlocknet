using FluentNHibernate.Data;
using System;

namespace DistLockNet.SqlBackend
{
    public class LockingObjectEntity : Entity
    {
        public virtual string AppId { get; set; }
        public virtual Guid LockerId { get; set; }
        public virtual Guid Seed { get; set; }
    }
}