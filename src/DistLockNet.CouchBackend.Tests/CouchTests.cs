using DistLockNet.CouchBackend.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Polly;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using Xunit;

namespace DistLockNet.CouchBackend.Tests
{
    public class CouchTests
    {
        private readonly IConfiguration _config;
        private readonly CouchConfig _couchConfig;
        private readonly Mock<ILogger> _logger;
        private readonly AutoResetEvent _aq = new AutoResetEvent(false);

        public CouchTests()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();
            _couchConfig = new CouchConfig("http://localhost:5984", "lock", "admin", "admin");
            _logger = new Mock<ILogger>();
        }

        [Fact]
        public void NoLockDB_SetNewLocker_LockAcquiredSuccessfully()
        {
            EmptyCouchDb();
            var lockAq = 0;

            var bnd = new CouchBackend(_couchConfig, _logger.Object);

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
        public void LockSetInDb_SetAnotherLocker_LockAcquireFailed()
        {
            EmptyCouchDb();
            var bnd0 = new CouchBackend(_couchConfig, _logger.Object);
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

            var bnd1 = new CouchBackend(_couchConfig, _logger.Object);
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

            var war = Policy.Handle<System.Exception>().WaitAndRetry(100, i => TimeSpan.FromMilliseconds(100));
            war.Execute(() => { if (lockWait < 2) { throw new System.Exception(); } });

            locker0.Halt();
            locker1.Halt();

            lockAq.Should().Be(1);
            lockWait.Should().Be(2);
        }

        private void EmptyCouchDb()
        {
            using var client = new HttpClient();
            var byteArray = Encoding.UTF8.GetBytes($"{_config["CouchDB:UserName"]}:{_config["CouchDB:Password"]}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            var del = client.DeleteAsync($"{_config["CouchDB:Url"]}/lock").Result;

            var put = client.PutAsync($"{_config["CouchDB:Url"]}/lock", null).Result;
        }
    }
}