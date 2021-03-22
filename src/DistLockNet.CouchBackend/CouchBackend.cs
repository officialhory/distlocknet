using DistLockNet.CouchBackend.Exception;
using DistLockNet.Interfaces;
using DistLockNet.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet.CouchBackend
{
    public class CouchBackend : ILockingBnd
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        private readonly string _couchUrl;
        private readonly string _couchUserName;
        private readonly string _couchPassword;
        private readonly bool _doAuth;

        public CouchBackend(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _couchUrl = _configuration["CouchDB:Url"];
            _couchUserName = _configuration["CouchDB:UserName"];
            _couchPassword = _configuration["CouchDB:Password"];
            _doAuth = !string.IsNullOrEmpty(_couchUserName) && !string.IsNullOrEmpty(_couchPassword);
        }

        public async Task<LockingObject> GetAsync(string application, CancellationToken ct)
        {
            try
            {
                using var client = new HttpClient();
                DoAuth(client);
                var response = await client.GetAsync($"{_couchUrl}/lock/{application}", ct);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var loe = JsonConvert.DeserializeObject<LockingObject>(content);

                    return loe;
                }

                throw new CouchBackendException(response.StatusCode.ToString());
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> AddAsync(LockingObject lo, CancellationToken ct)
        {
            try
            {
                using var client = new HttpClient();
                DoAuth(client);
                var body = new StringContent(JsonConvert.SerializeObject(lo));
                var response = await client.PutAsync($"{_couchUrl}/lock/{lo.AppId}", body, ct);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                throw new CouchBackendException(response.StatusCode.ToString());
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AllocateAsync(LockingObject lo, CancellationToken ct)
        {
            return await ModifyAsync(lo, x => x.AppId == lo.AppId, ct);
        }

        public async Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct)
        {
            return await ModifyAsync(lo, x => x.AppId == lo.AppId && x.LockerId == lo.LockerId, ct);
        }

        private async Task<bool> ModifyAsync(LockingObject lo, Func<LockingObjectEntity, bool> predicate, CancellationToken ct)
        {
            try
            {
                using var client = new HttpClient();
                DoAuth(client);
                var response = await client.GetAsync($"{_couchUrl}/lock/{lo.AppId}", ct);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var savedEntities = JsonConvert.DeserializeObject<IList<LockingObjectEntity>>(content);

                    if (savedEntities != null && savedEntities.Any())
                    {
                        var lockingObject = savedEntities.First(predicate);
                        lockingObject.LockerId = lo.LockerId;
                        lockingObject.Seed = lo.Seed;

                        var body = new StringContent(JsonConvert.SerializeObject(lockingObject));
                        var putResponse = await client.PutAsync($"{_couchUrl}/lock/{lockingObject.AppId}", body, ct);

                        if (putResponse.IsSuccessStatusCode)
                        {
                            return true;
                        }

                        throw new CouchBackendException($"LockingObjectEntity may be changed: {lo.AppId}, {lo.LockerId}");
                    }
                }

                throw new CouchBackendException($"LockingObjectEntity could not be found: {lo.AppId}, {lo.LockerId}");
            }
            catch
            {
                return false;
            }
        }

        private void DoAuth(HttpClient client)
        {
            if (!_doAuth)
            {
                return;
            }

            var byteArray = Encoding.ASCII.GetBytes($"{_couchUserName}:{_couchPassword}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
    }
}