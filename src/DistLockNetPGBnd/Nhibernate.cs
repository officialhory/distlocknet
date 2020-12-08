using DistLockNet.Interfaces;
using DistLockNet.Models;
using Microsoft.Extensions.Configuration;
using NHibernate;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet.SqlBackend
{
    public class Nhibernate: ILockingBnd
    {
        private readonly IConfiguration _config;

        public Nhibernate(IConfiguration config)
        {
            _config = config;
        }

        private void ExecuteTransaction(Action<ISession> action)
        {
            //try
            ////create session
            ////open session
            ////create transaction

            //action.Invoke(ssss);


            //// commit transaction
            //// free session
            //catch

            try
            {

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

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
