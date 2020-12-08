using System;

namespace DistLockNet.Interfaces
{
    public interface ILocker
    {
        void Lock();

        void Halt();

        Action<string> OnLockAcquired { get; }

        Action<string> OnLockLost { get; }
    }
}