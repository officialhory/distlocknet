using System;

namespace DistLockNet.Models
{
    public class LockingObject
    {
        public string AppId { get; }
        public Guid LockerId { get; }
        public Guid Seed { get; }

        public LockingObject(string appId, Guid lockerId, Guid seed)
        {
            AppId = appId;
            LockerId = lockerId;
            Seed = seed;
        }

        public override bool Equals(object obj)
        {
            return Equals((LockingObject) obj);
        }

        protected bool Equals(LockingObject other)
        {
            if (other == null)
            {
                return false;
            }

            return AppId == other.AppId && LockerId.Equals(other.LockerId) && Seed.Equals(other.Seed);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AppId != null ? AppId.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ LockerId.GetHashCode();
                hashCode = (hashCode * 397) ^ Seed.GetHashCode();
                return hashCode;
            }
        }
    }
}