using DistLockNet.Exceptions;
using DistLockNet.Interfaces;
using DistLockNet.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
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

        private int _lockAq = 0;
        private int _lockLost = 0;
        private int _lockFail = 0;

        public LockerTests()
        {

            var conf = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true).Build();

            _lockingBndMock = new Mock<ILockingBnd>();
            _lockingBnd = _lockingBndMock.Object;

            _locker = new Locker(conf, _lockingBnd)
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
        }

        private void Reset()
        {
            _aq.Reset();
            _lo.Reset();
            _lockAq = 0;
            _lockLost = 0;
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

            _lockFail.Should().BeGreaterOrEqualTo(1);
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

            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LockingObject
                {
                    AppId = "myApp",
                    Seed = Guid.NewGuid(),
                    LockerId = Guid.NewGuid()
                });

            _lockingBndMock.Setup(l => l.UpdateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _locker.Lock();

            _aq.WaitOne(2000);

            _locker.Halt();

            _lockAq.Should().Be(1);
        }

        [Fact(DisplayName = "Locking Object exists, fail to lock")]
        public void LockingObject_TryToLock_Fail()
        {
            Reset();

            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LockingObject
                {
                    AppId = "myApp",
                    Seed = Guid.NewGuid(),
                    LockerId = Guid.NewGuid()
                });
            _lockingBndMock.Setup(l => l.UpdateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _locker.Lock();

            _lf.WaitOne(2000);

            _locker.Halt();

            _lockFail.Should().BeGreaterOrEqualTo(1);
        }

        [Fact(DisplayName = "Locking Object exists, fail to update in heartbeat")]
        public void LockingObject_CantUpdateHeartbeat_Fail()
        {
            Reset();

            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LockingObject
                {
                    AppId = "myApp",
                    Seed = Guid.NewGuid(),
                    LockerId = Guid.NewGuid()
                });
            _lockingBndMock.SetupSequence(l => l.UpdateAsync(It.IsAny<LockingObject>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(false)
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

        [Fact(DisplayName = "Wrong timeout value")]
        public void WrongTimeoutValue_Exception()
        {
            var conf = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Uri.UnescapeDataString((new UriBuilder(Assembly.GetExecutingAssembly().CodeBase)).Path)))
                .AddJsonFile("wrong_settings.json", optional: true, reloadOnChange: true).Build();

            Action action = () => new Locker(conf, _lockingBnd);

            action.Should().Throw<LockerException>();
        }
    }
}
