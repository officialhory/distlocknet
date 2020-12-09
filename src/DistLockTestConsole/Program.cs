using System;
using System.IO;
using System.Reflection;
using DistLockNet;
using DistLockNet.SqlBackend;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DistLockTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"App-{Guid.NewGuid()} Started.");


            var conf = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();

            ILogger logger = new LoggerConfiguration()
                .CreateLogger();

            var bnd = new SqlBackend(conf, logger);

            var locker = new Locker(conf, bnd);

            locker.OnLockFail = (str) => logger.Debug($"Lock Fail: {str}");
            locker.OnLockLost = (str) => logger.Debug($"Lock Lost: {str}");
            locker.OnLockAcquired = (str) => logger.Debug($"Lock Acquired: {str}");
        }
    }
}
