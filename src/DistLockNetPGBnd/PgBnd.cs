using DistLockNet.Interfaces;
using DistLockNet.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNetPGBnd
{
    public class PgBnd: ILockingBnd
    {

        public Task<LockingObject> GetAsync(string application, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddAsync(LockingObject lo, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
