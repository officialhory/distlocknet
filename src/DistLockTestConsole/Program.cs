using DistLockNet;
using DistLockNet.CouchBackend;
using DistLockNet.CouchBackend.Model;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Reflection;

namespace DistLockTestConsole
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"App-{Guid.NewGuid()} Started.");

            var conf = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();

            ILogger logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            //var dps = new DataProviderSelector(conf);
            //var cf = new DbContextFactory(dps);
            //var bnd = new SqlBackend(cf, logger);


            var bnd = new CouchBackend(new CouchConfig
            {
                DbName = "lock",
                DoAuth = true,
                Password = "admin",
                Url = "http://localhost:5984",
                UserName = "admin"
            }, logger);

            var locker = new Locker(conf, bnd, logger)
            {
                OnLockFail = (str) =>
                {
                    logger.Information($"Lock Fail: {str}");
                },
                OnLockLost = (str) =>
                {
                    logger.Information($"Lock Lost: {str}");
                },
                OnLockAcquired = (str) =>
                {
                    logger.Information($"Lock Acquired: {str}");
                }
            };

            locker.Lock();

            Console.ReadLine();

        }

        public class CouchConfig : ICouchConfig
        {
            public string Url { get; set; }
            public string DbName { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public bool DoAuth { get; set; }
        }
    }
}
