﻿namespace DistLockNet.CouchBackend.Model
{
    public class CouchConfig
    {
        public string Url { get; }
        public string UserName { get; }
        public string Password { get; }
        public bool DoAuth { get; }

        public CouchConfig(string url, string userName, string password)
        {
            Url = url;
            UserName = userName;
            Password = password;
            DoAuth = !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password);
        }
    }
}