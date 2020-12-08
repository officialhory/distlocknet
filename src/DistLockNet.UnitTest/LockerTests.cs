using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DistLockNet.Interfaces;
using DistLockNet.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DistLockNet.UnitTest
{
    public class LockerTests
    {
        private Locker _locker;
        private ILockingBnd _lockingBnd;
        private Mock<ILockingBnd> _lockingBndMock;
        private int _lockAq = 0;
        private int _lockLost = 0;

        public LockerTests()
        {

            var conf = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();

            _lockingBndMock = new Mock<ILockingBnd>();
            _lockingBnd = _lockingBndMock.Object;

            _locker = new Locker(conf, _lockingBnd)
            {
                OnLockAcquired = (str) => { _lockAq++; },
                OnLockLost = (str) => { _lockLost++; }
            };
        }

        private void ResetCallOutCounters()
        {
            _lockAq = 0;
            _lockLost = 0;
        }

        [Fact(DisplayName = "No Locking Object in Bnd, lock successfully")]
        public async Task NoLockingObject_TryToLoack_Success()
        {
            ResetCallOutCounters();

            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LockingObject)null);

            _lockingBndMock.Setup(l => l.AddAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _locker.Lock();

            await Task.Delay(2000);

            _lockAq.Should().Be(1);
        }
    }
}
