namespace DistLockNet.CouchBackend.Model
{
    public interface ICouchConfig
    {
        public string Url { get; }
        public string DbName { get; }
        public string UserName { get; }
        public string Password { get; }
        public bool DoAuth { get; }
    }
}