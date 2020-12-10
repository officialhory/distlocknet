using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Xunit;

namespace DistLockNet.SqlBackend.Tests
{
    public class SqliteTests
    {
        private readonly IConfiguration _config;
        
        private readonly Mock<ILogger> _logger;
        
        private readonly AutoResetEvent _aq = new AutoResetEvent(false);
        private readonly AutoResetEvent _lo = new AutoResetEvent(false);
        private readonly AutoResetEvent _lf = new AutoResetEvent(false);

        private int _lockAq = 0;
        private int _lockLost = 0;
        private int _lockFail = 0;

        public SqliteTests()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();

            _logger = new Mock<ILogger>();

        }

        private void Reset()
        {
            _lockAq = 0;
            _lockLost = 0;
            _lockFail = 0;
        }

        [Fact]
        public void Sqlite_Success()
        {
            Reset();
            var bnd = new SqlBackend(_config, _logger.Object);

            var locker = new Locker(_config, bnd, _logger.Object)
            {
                OnLockAcquired = (str) =>
                {
                    _lockAq++;
                    _aq.Set();
                },
                OnLockLost = (str) =>
                {
                    _lockLost++;
                    _lo.Set();
                },
                OnLockFail = (str) =>
                {
                    _lockFail++;
                    _lf.Set();
                }
            };

            locker.Lock();

            _aq.WaitOne(10000);

            locker.Halt();

            _lockAq.Should().Be(1);
        }
    }
}
