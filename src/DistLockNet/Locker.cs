using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DistLockNet.Interfaces;

namespace DistLockNet
{
    public class Locker: ILocker
    {
        private readonly string _appId;

        public Action<string> OnLockAcquired { get; }
        public Action<string> OnLockLost { get; }

        public Locker(string appId)
        {
            _appId = appId;
        }

        public async Task LockAsync()
        {
            throw new NotImplementedException();
        }
      
    }
}
