using DistLockNet.Exceptions;
using DistLockNet.Interfaces;
using DistLockNet.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet
{
    public class Locker : ILocker
    {
        private readonly string _appId;
        private readonly ILockingBnd _bnd;
        private readonly ILogger _logger;

        public Action<string> OnLockAcquired { get; set; }
        public Action<string> OnLockLost { get; set; }
        public Action<string> OnLockFail { get; set; }
        public Action<string> OnWaitForUnlock { get; set; }

        private readonly CancellationTokenSource _ct;
        private readonly Guid _lockerId;
        private LockingObject _lo;
        private readonly int _heartbeat;
        private int _seedCounter = 0;
        private int _failCounter = 0;

        private readonly int _missedHeartBeatCount;

        public Locker(IConfiguration config, ILockingBnd bnd, ILogger logger)
        {
            _logger = logger;

            _appId = config.GetValue<string>("Locker:ApplicationId");
            var timeoutSeconds = config.GetValue<int>("Locker:TimeOutSeconds");
            _missedHeartBeatCount = config.GetValue<int>("Locker:MissedHeartBeatCountBeforeExpire");

            _heartbeat = timeoutSeconds / _missedHeartBeatCount * 1000;
            if (timeoutSeconds < 5)
            {
                throw new LockerException("Timeout value is too small, should be greater than 5 seconds.");
            }

            _logger.Information(@$"Locker created: {JsonConvert.SerializeObject(new
            {
                TimeoutSeconds = timeoutSeconds,
                HeartBeatSeconds = _heartbeat / 1000,
                MissedHeartBeatCount = _missedHeartBeatCount
            })}");

            _bnd = bnd;
            _ct = new CancellationTokenSource();
            _lockerId = Guid.NewGuid();
        }

        public void Lock()
        {
            Task.Run(async () =>
            {
                _lo = await _bnd.GetAsync(_appId, _ct.Token);

                if (_lo == null)
                {
                    _logger.Debug("No LockingObject found");

                    _lo = LockingObjectFactory();

                    _logger.Debug("Try to Add LockingObject");

                    if (await _bnd.AddAsync(_lo, _ct.Token))
                    {
                        _logger.Debug("LockingObject Added");

                        OnLockAcquired?.Invoke(_appId);
                        StartHeartbeat();
                        return;
                    }

                    _logger.Debug("Fail to Add LockingObject");

                    OnLockFail?.Invoke(_appId);
                }

                while (!_ct.IsCancellationRequested)
                {
                    _logger.Debug("Getting LockingObject");

                    var lo = await _bnd.GetAsync(_appId, _ct.Token);

                    _logger.Debug("Got LockingObject");

                    if (CheckLockExpired(lo))
                    {
                        _logger.Debug("LockingObject Expired");

                        _lo = LockingObjectFactory();

                        _logger.Debug("Try Allocate LockingObject ");

                        if (await _bnd.AllocateAsync(_lo, _ct.Token))
                        {
                            _logger.Debug("LockingObject Allocated successfully");

                            OnLockAcquired?.Invoke(_appId);
                            StartHeartbeat();
                            return;
                        }

                        _logger.Debug("LockingObject Allocate fail");
                        OnLockFail?.Invoke(_appId);
                    }
                    _logger.Debug("Waiting for Lock ...");
                    OnWaitForUnlock?.Invoke(_appId);
                    //await Task.Delay(_heartbeat);
                }
            }, _ct.Token);
        }

        public bool CheckLockExpired(LockingObject lo)
        {
            if (_lo.Equals(lo))
            {
                _seedCounter++;
                return _seedCounter >= _missedHeartBeatCount;
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

                    _logger.Debug("Sending HeartBeat...");

                    if (await _bnd.UpdateAsync(LockingObjectFactory(), _ct.Token))
                    {
                        _logger.Debug("HeartBeat sent");

                        _failCounter = 0;
                    }
                    else
                    {
                        _logger.Debug("HeartBeat sending fail");

                        _failCounter++;

                        OnLockFail?.Invoke(_appId);

                        if (_failCounter >= _missedHeartBeatCount)
                        {
                            _logger.Debug("Lock lost");

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
            return new LockingObject(
                _appId,
                _lockerId,
                Guid.NewGuid());
        }
    }
}
