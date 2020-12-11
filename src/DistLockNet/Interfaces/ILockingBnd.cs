using DistLockNet.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet.Interfaces
{
    public interface ILockingBnd
    {
        Task<LockingObject> GetAsync(string application, CancellationToken ct);
        Task<bool> AddAsync(LockingObject lo, CancellationToken ct);
        Task<bool> AllocateAsync(LockingObject lo, CancellationToken ct);
        Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct);
    }
}
