using System;
using System.Threading.Tasks;
using DistLockNet.Models;

namespace DistLockNet.Interfaces
{
    public interface ILockingBnd
    {
        Task<LockingObject> GetAsync(string application);
        Task<bool> AddAsync(string application, Guid lockerId, Guid seed);
        Task<bool> UpdateAsync(string application, Guid lockerId, Guid seed);
    }
}
