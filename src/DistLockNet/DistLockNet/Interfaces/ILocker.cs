using DistLockNet.Models;
using System;
using System.Threading.Tasks;

namespace DistLockNet.Interfaces
{
    public interface ILocker
    {
        LockingObject LockingObject { get; set; }

        Task CreateLock();

        Task<LockingObject> GetLock();

        Task UpdateLock(Guid lockingObjectId);
    }
}