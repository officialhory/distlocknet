using DistLockNet.Interfaces;
using DistLockNet.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet.RedisBackend
{
    public class RedisBnd : ILockingBnd
    {
        public Task<LockingObject> GetAsync(string application, CancellationToken ct)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> AddAsync(LockingObject lo, CancellationToken ct)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct)
        {
            throw new System.NotImplementedException();
        }
    }
}
