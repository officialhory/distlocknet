using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DistLockNet.CouchBackend.Tests
{
    public class CouchTests
    {
        private readonly IConfiguration _config;
        private readonly Mock<ILogger> _logger;
        private readonly AutoResetEvent _aq = new AutoResetEvent(false);

        public CouchTests()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();

            _logger = new Mock<ILogger>();
        }

        [Fact]
        public void Sqlite_Success()
        {
            var lockAq = 0;

            var bnd = new CouchBackend(_config, _logger.Object);

            var locker = new Locker(_config, bnd, _logger.Object)
            {
                OnLockAcquired = (str) =>
                {
                    lockAq++;
                    _aq.Set();
                }
            };

            locker.Lock();
            _aq.WaitOne(10000);
            locker.Halt();


            lockAq.Should().Be(1);
        }

        [Fact]
        public void TwoLockerParallel_Success()
        {
            var bnd0 = new CouchBackend(_config, _logger.Object);
            var lockAq = 0;
            var lockWait = 0;

            var locker0 = new Locker(_config, bnd0, _logger.Object)
            {
                OnLockAcquired = (str) =>
                {
                    lockAq++;
                    _aq.Set();
                },
                OnWaitForUnlock = (str) =>
                {
                    lockWait++;
                }
            };

            var bnd1 = new CouchBackend(_config, _logger.Object);
            var locker1 = new Locker(_config, bnd1, _logger.Object)
            {
                OnLockAcquired = (str) =>
                {
                    lockAq++;
                    _aq.Set();
                },
                OnWaitForUnlock = (str) =>
                {
                    lockWait++;
                }
            };

            locker0.Lock();
            _aq.WaitOne(6000);
            locker1.Lock();
            locker0.Halt();
            locker1.Halt();

            lockAq.Should().Be(1);
            lockWait.Should().Be(2);
        }

        private async Task EmptyCouchDb()
        {
            using var client = new HttpClient();
            await client.DeleteAsync($"{_config["CouchDB:Url"]}/lock");
        }
    }
}