using DistLockNet.Models;
using System;
using System.Threading.Tasks;

namespace DistLockNet.Interfaces
{
    public interface ILocker
    {
        Task LockAsync();

        Action<string> OnLockAcquired { get; }

        Action<string> OnLockLost { get; }
    }
}