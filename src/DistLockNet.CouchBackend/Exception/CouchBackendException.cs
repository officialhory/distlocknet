namespace DistLockNet.CouchBackend.Exception
{
    public class CouchBackendException : System.Exception
    {
        public int ErrorCode { get; }
        public string ErrorMessage { get; }

        public CouchBackendException(int statusCode, string message) : base(message)
        {
            ErrorCode = statusCode;
            ErrorMessage = message;
        }
    }
}
