using DistLockNet;
using DistLockNet.SqlBackend;
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
                .CreateLogger();

            var bnd = new SqlBackend(conf, logger);

            var locker = new Locker(conf, bnd)
            {
                OnLockFail = (str) => logger.Debug($"Lock Fail: {str}"),
                OnLockLost = (str) => logger.Debug($"Lock Lost: {str}"),
                OnLockAcquired = (str) => logger.Debug($"Lock Acquired: {str}")
            };
            

        }
    }
}
