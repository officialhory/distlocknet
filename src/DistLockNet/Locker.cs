using DistLockNet.Exceptions;
using DistLockNet.Interfaces;
using DistLockNet.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet
{
    public class Locker : ILocker
    {
        private readonly string _appId;
        private readonly ILockingBnd _bnd;

        public Action<string> OnLockAcquired { get; set; }
        public Action<string> OnLockLost { get; set; }

        private readonly CancellationTokenSource _ct;
        private readonly Guid _lockerId;
        private LockingObject _lo;
        private readonly int _timeoutSeconds;
        private readonly int _heartbeat;
        private int _seedCounter = 0;
        private int _failCounter = 0;

        private const int EXPIRATION_COUNT = 5;

        public Locker(IConfiguration config, ILockingBnd bnd)
        {
            _appId = config.GetValue<string>("Locker:ApplicationId");
            _timeoutSeconds = config.GetValue<int>("Locker:TimeOutSeconds");
            _heartbeat = _timeoutSeconds / EXPIRATION_COUNT;
            if (_timeoutSeconds < 5)
            {
                throw new LockerException("Timeout value is too small, should be greater than 5 seconds.");
            }
            _bnd = bnd;
            _ct = new CancellationTokenSource();
            _lockerId = Guid.NewGuid();
        }

        public void Lock()
        {
            Task.Run(async () =>
            {
                var lo = await _bnd.GetAsync(_appId, _ct.Token);
                if (lo == null)
                {
                    _lo = LockingObjectFactory();

                    if (await _bnd.AddAsync(_lo, _ct.Token))
                    {
                        StartHeartbeat();
                        OnLockAcquired?.Invoke(_appId);
                        return;
                    }
                }

                while (!_ct.IsCancellationRequested)
                {
                    lo = await _bnd.GetAsync(_appId, _ct.Token);
                    if (CheckLockExpired(lo))
                    {
                        _lo = LockingObjectFactory();

                        if (await _bnd.UpdateAsync(_lo, _ct.Token))
                        {
                            StartHeartbeat();
                            OnLockAcquired?.Invoke(_appId);
                            return;
                        }
                    }

                    await Task.Delay(_heartbeat);
                }
            }, _ct.Token);
        }

        public bool CheckLockExpired(LockingObject lo)
        {
            if (_lo.Equals(lo))
            {
                _seedCounter++;
                return _seedCounter >= EXPIRATION_COUNT;
            }

            _lo = lo;
            _seedCounter = 0;
            return false;
        }

        private void StartHeartbeat()
        {
            Task.Run(async () =>
            {
                while (!_ct.Token.IsCancellationRequested)
                {
                    await Task.Delay(_heartbeat);

                    if (await _bnd.UpdateAsync(LockingObjectFactory(), _ct.Token))
                    {
                        _failCounter = 0;
                    }
                    else
                    {
                        _failCounter++;
                        if (_failCounter >= EXPIRATION_COUNT)
                        {
                            OnLockLost?.Invoke(_appId);
                            return;
                        }
                    }
                }
            }, _ct.Token);
        }

        public void Halt()
        {
            _ct.Cancel();
        }

        private LockingObject LockingObjectFactory()
        {
            return new LockingObject
            {
                AppId = _appId,
                Seed = Guid.NewGuid(),
                LockerId = _lockerId
            };
        }
    }
}
