using System;

namespace DistLockNet.Exceptions
{
    public class LockerException : Exception
    {
        public LockerException(string message): base(message) { }
    }
}