using System;

namespace DistLockNet.SqlBackend.Exception
{
    public class SqlBackendException: System.Exception
    {
        public SqlBackendException(string msg): base(msg)
        {
            
        }
    }
}
