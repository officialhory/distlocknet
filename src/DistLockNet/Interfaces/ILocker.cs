using System;

namespace DistLockNet.Interfaces
{
    public interface ILocker
    {
        Action<string> OnLockAcquired { get; }

        Action<string> OnLockLost { get; }

        void Lock();

        void Halt();
    }
}