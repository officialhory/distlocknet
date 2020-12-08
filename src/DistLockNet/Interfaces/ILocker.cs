using System;

namespace DistLockNet.Interfaces
{
    public interface ILocker
    {
        Action<string> OnLockAcquired { get; set; }

        Action<string> OnLockLost { get; set; }

        void Lock();

        void Halt();
    }
}