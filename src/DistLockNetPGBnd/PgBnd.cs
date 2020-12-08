using DistLockNet.Interfaces;
using DistLockNet.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNetPGBnd
{
    public class PgBnd: ILockingBnd
    {
        private readonly IConfiguration _config;

        public PgBnd(IConfiguration config)
        {
            _config = config;
        }

        //private void ExecuteTransaction(Action<ISession> action)
        //{
        //      try
        //    //create session
        //    //open session
        //    //create transaction

        //    action.Invoke(ssss);

            
        //    // commit transaction
        //    // free session
        //     catch
        //}

        public Task<LockingObject> GetAsync(string application, CancellationToken ct)
        {

            // return null if any issue
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
