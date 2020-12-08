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
            var conf = new ConfigurationBuilder().SetBasePath("/")
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

            _lockingBnd = new Mock<ILockingBnd>().Object;

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

        [Fact]
        public async Task Test1()
        {
            ResetCallOutCounters();

            _lockingBndMock.Setup(l => l.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LockingObject)null);

            _locker.Lock();

            await Task.Delay(2000);

            _lockAq.Should().Be(1);
        }
    }
}
