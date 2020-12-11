using DistLockNet.Exceptions;
using DistLockNet.Interfaces;
using DistLockNet.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Xunit;

namespace DistLockNet.UnitTest
{
    public class LockerTests
    {
        private readonly Locker _locker;
        private readonly ILockingBnd _lockingBnd;
        private readonly Mock<ILockingBnd> _lockingBndMock;
        private readonly AutoResetEvent _aq = new AutoResetEvent(false);
        private readonly AutoResetEvent _lo = new AutoResetEvent(false);
        private readonly AutoResetEvent _lf = new AutoResetEvent(false);
        private readonly AutoResetEvent _lw = new AutoResetEvent(false);
        private readonly ILogger _logger;

        private int _lockAq;
        private int _lockLost;
        private int _lockFail;
        private int _lockWait;

        public LockerTests()
        {

            var conf = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();

            _lockingBndMock = new Mock<ILockingBnd>();
            _lockingBnd = _lockingBndMock.Object;

            _logger = new Mock<ILogger>().Object;

            _locker = new Locker(conf, _lockingBnd, _logger)
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
                },
                OnWaitForUnlock = (str) =>
                {
                    _lockWait++;
                    _lw.Set();
                }
            };
        }

        private void Reset()
        {
            _lockAq = 0;
            _lockLost = 0;
            _lockFail = 0;
            _lockWait = 0;
        }

        [Fact(DisplayName = "No Locking Object exists, lock successfully")]
        public void NoLockingObject_TryToLock_Success()
        {
            Reset();

            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LockingObject)null);

            _lockingBndMock.Setup(l => l.AddAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _locker.Lock();

            _aq.WaitOne(2000);

            _locker.Halt();

            _lockAq.Should().Be(1);
        }

        [Fact(DisplayName = "No Locking Object exists, fail to lock")]
        public void NoLockingObject_TryToLock_Fail()
        {
            Reset();

            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LockingObject)null);

            _lockingBndMock.Setup(l => l.AddAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _locker.Lock();

            _lf.WaitOne(2000);

            _locker.Halt();

            _lockFail.Should().Be(1);
        }

        [Fact(DisplayName = "No Locking Object exists, fail to update in heartbeat")]
        public void NoLockingObject_CantUpdateHeartbeat_Fail()
        {
            Reset();

            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LockingObject)null);
            _lockingBndMock.Setup(l => l.AddAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _lockingBndMock.Setup(l => l.UpdateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _locker.Lock();

            _aq.WaitOne(2000);
            
            _lf.WaitOne(2000);
            _lf.WaitOne(2000);
            _lf.WaitOne(2000);
            _lf.WaitOne(2000);
            _lf.WaitOne(2000);

            _lo.WaitOne(2000);

            _locker.Halt();

            _lockAq.Should().Be(1);
            _lockLost.Should().Be(1);
            _lockFail.Should().Be(5);
        }

        [Fact(DisplayName = "Locking Object exists, lock successful")]
        public void LockingObject_TryToLock_Success()
        {
            Reset();

            var loId = Guid.NewGuid();
            var seedId = Guid.NewGuid();
            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LockingObject("myApp", loId, seedId));

            _lockingBndMock
                .SetupSequence(l => l.AllocateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _lockingBndMock.Setup(l => l.UpdateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _locker.Lock();

            _aq.WaitOne(5000);

            _locker.Halt();

            _lockAq.Should().Be(1);
        }

        [Fact(DisplayName = "Locking Object exists, fail to lock")]
        public void LockingObject_TryToLock_Fail()
        {
            Reset();

            var loId = Guid.NewGuid();
            var seedId = Guid.NewGuid();
            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LockingObject("myApp", loId, seedId));

            _lockingBndMock
                .SetupSequence(l => l.AllocateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _locker.Lock();

            _lf.WaitOne(5000);

            _locker.Halt();

            _lockFail.Should().Be(1);
        }

        [Fact(DisplayName = "Locking Object exists, fail to update in heartbeat")]
        public void LockingObject_CantUpdateHeartbeat_Fail()
        {
            Reset();

            var loId = Guid.NewGuid();
            var seedId = Guid.NewGuid();
            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LockingObject("myApp", loId, seedId));

            _lockingBndMock
                .SetupSequence(l => l.AllocateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _lockingBndMock.SetupSequence(l => l.UpdateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(false);

            _locker.Lock();

            _aq.WaitOne(5000);

            _lf.WaitOne(2000);
            _lf.WaitOne(2000);
            _lf.WaitOne(2000);
            _lf.WaitOne(2000);
            _lf.WaitOne(2000);

            _lo.WaitOne(2000);

            _locker.Halt();

            _lockAq.Should().Be(1);
            _lockLost.Should().Be(1);
            _lockFail.Should().Be(5);
        }

        [Fact(DisplayName = "Locking Object exists, wait for lock")]
        public void LockingObject_WaitForLock_Success()
        {
            Reset();

            var loId = Guid.NewGuid();
            var seedId = Guid.NewGuid();
            _lockingBndMock.SetupSequence(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LockingObject("myApp", loId, seedId))
                .ReturnsAsync(new LockingObject("myApp2", loId, seedId));

            _lockingBndMock
                .SetupSequence(l => l.AllocateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _lockingBndMock.Setup(l => l.UpdateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _locker.Lock();

            _lw.WaitOne(5000);

            _locker.Halt();

            _lockWait.Should().Be(1);
        }

        [Fact(DisplayName = "Wrong timeout value")]
        public void WrongTimeoutValue_Exception()
        {
            var conf = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("wrong_settings.json", optional: true, reloadOnChange: true).Build();

            Action action = () => new Locker(conf, _lockingBnd, _logger);

            action.Should().Throw<LockerException>();
        }
    }
}
