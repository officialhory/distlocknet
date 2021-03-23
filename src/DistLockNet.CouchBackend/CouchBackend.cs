using DistLockNet.CouchBackend.Exception;
using DistLockNet.CouchBackend.Model;
using DistLockNet.Interfaces;
using DistLockNet.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet.CouchBackend
{
    public class CouchBackend : ILockingBnd
    {
        private readonly ICouchConfig _config;
        private readonly ILogger _logger;

        public CouchBackend(ICouchConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<LockingObject> GetAsync(string application, CancellationToken ct)
        {
            _logger.Verbose("Trying to get LockingObject for application: {Application}", application);
            try
            {
                using var client = new HttpClient();
                DoAuth(client);
                var response = await client.GetAsync($"{_config.Url}/{_config.DbName}/{application}", ct);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var loe = JsonConvert.DeserializeObject<LockingObject>(content);
                    _logger.Verbose("Successfully get LockingObject for application: {Application}", application);
                    return loe;
                }

                throw new CouchBackendException((int)response.StatusCode, response.Content.ToString());
            }
            catch (CouchBackendException e)
            {
                _logger.Verbose(e, "Error during getting LockingObject. StatusCode: {ErrorCode}. Message: {ErrorMessage}", e.ErrorCode, e.ErrorMessage);
                return null;
            }
        }

        public async Task<bool> AddAsync(LockingObject lo, CancellationToken ct)
        {
            _logger.Verbose("Trying to add LockingObject for application: {Application}", lo.AppId);
            try
            {
                using var client = new HttpClient();
                DoAuth(client);
                var body = new StringContent(JsonConvert.SerializeObject(lo));
                var response = await client.PutAsync($"{_config.Url}/{_config.DbName}/{lo.AppId}", body, ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.Verbose("Successfully add LockingObject for application: {Application}", lo.AppId);
                    return true;
                }

                throw new CouchBackendException((int)response.StatusCode, response.Content.ToString());
            }
            catch (CouchBackendException e)
            {
                _logger.Verbose(e, "Error during adding LockingObject. StatusCode: {ErrorCode}. Message: {ErrorMessage}", e.ErrorCode, e.ErrorMessage);
                return false;
            }
        }

        public async Task<bool> AllocateAsync(LockingObject lo, CancellationToken ct)
        {
            _logger.Verbose("Trying to allocate LockingObject for application: {Application}", lo.AppId);
            return await ModifyAsync(lo, false, ct);
        }

        public async Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct)
        {
            _logger.Verbose("Trying to update LockingObject for application: {Application}", lo.AppId);
            return await ModifyAsync(lo, true, ct);
        }

        private async Task<bool> ModifyAsync(LockingObject lo, bool doCheckLocker, CancellationToken ct)
        {
            try
            {
                using var client = new HttpClient();
                DoAuth(client);
                var response = await client.GetAsync($"{_config.Url}/{_config.DbName}/{lo.AppId}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    throw new CouchBackendException((int)response.StatusCode,
                        "Cannot get LockingObject. Reason: " + response.Content);
                }

                _logger.Verbose("Successfully get LockingObject for application: {Application}", lo.AppId);
                var content = await response.Content.ReadAsStringAsync();
                var entity = JsonConvert.DeserializeObject<LockingObjectEntity>(content);

                if (doCheckLocker && entity.LockerId != lo.LockerId)
                {
                    return false;
                }

                entity.LockerId = lo.LockerId;
                entity.Seed = lo.Seed;

                var body = new StringContent(JsonConvert.SerializeObject(entity));
                response = await client.PutAsync($"{_config.Url}/{_config.DbName}/{entity.AppId}", body, ct);

                if (!response.IsSuccessStatusCode)
                {
                    throw new CouchBackendException((int)response.StatusCode,
                        "Cannot modify LockingObject. Reason: " + response.Content);
                }

                _logger.Verbose("Successfully modify LockingObject for application: {Application}", lo.AppId);
                return true;
            }
            catch (CouchBackendException e)
            {
                _logger.Verbose(e, "Error during updating LockingObject. StatusCode: {ErrorCode}. Message: {ErrorMessage}", e.ErrorCode, e.ErrorMessage);
                return false;
            }
        }

        private void DoAuth(HttpClient client)
        {
            if (!_config.DoAuth)
            {
                return;
            }

            var byteArray = Encoding.UTF8.GetBytes($"{_config.UserName}:{_config.Password}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
    }
}