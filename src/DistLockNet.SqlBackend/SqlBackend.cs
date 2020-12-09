using DistLockNet.Interfaces;
using DistLockNet.Models;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.Extensions.Configuration;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using Serilog;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet.SqlBackend
{
    public class SqlBackend : ILockingBnd
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly string _databaseType;

        public SqlBackend(IConfiguration config, ILogger logger)
        {
            _logger = logger;
            _connectionString = config.GetValue<string>("ConnectionString");
            _databaseType = config.GetValue<string>("dbtype");
        }

        public async Task<LockingObject> GetAsync(string application, CancellationToken ct)
        {

            try
            {
                LockingObjectEntity loe = null;
                await ExecuteTransactionAsync(async session =>
                {
                    loe = await session.Query<LockingObjectEntity>().Where(i => i.AppId == application).FirstOrDefaultAsync(ct);
                }, ct);

                return new LockingObject(loe.AppId, loe.LockerId, loe.Seed);
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
                await ExecuteTransactionAsync(async session =>
                {
                    await session.SaveAsync(new LockingObjectEntity
                    {
                        AppId = lo.AppId,
                        LockerId = lo.LockerId,
                        Seed = lo.Seed
                    }, ct);

                }, ct);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct)
        {
            try
            {
                await ExecuteTransactionAsync(async session =>
                {
                    var loe = await session.Query<LockingObjectEntity>().Where(i => i.AppId == lo.AppId && i.LockerId == lo.LockerId).FirstOrDefaultAsync(ct);
                    if (loe == null)
                    {
                        throw new NullReferenceException($"LockingObjectEntity does not exist: {lo.AppId}, {lo.LockerId}");
                    }

                    loe.Seed = lo.Seed;
                    await session.SaveAsync(loe, ct);
                }, ct);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private Configuration DatabaseConfiguration(string dbType)
        {
            var cfg = Fluently.Configure();

            switch (dbType.ToLower())
            {
                case "oracle":
                    cfg.Database(OracleManagedDataClientConfiguration.Oracle10.ConnectionString(_connectionString));
                    break;

                case "postgres":
                    cfg.Database(PostgreSQLConfiguration.PostgreSQL82.ConnectionString(_connectionString));
                    break;

                case "sqlite":
                    cfg.Database(SQLiteConfiguration.Standard.ConnectionString(_connectionString));
                    break;

                default:
                    _logger.Error("Unknown database type: {type}.", dbType);
                    break;
            }

            return cfg.ExposeConfiguration(c => new SchemaUpdate(c).Execute(true, true))
                .Mappings(map => map.FluentMappings.AddFromAssemblyOf<LockingObjectEntity>())
                .BuildConfiguration();
        }

        private async Task ExecuteTransactionAsync(Func<ISession, Task> exec, CancellationToken ct)
        {
            try
            {
                var dbConfig = DatabaseConfiguration(_databaseType);
                var session = dbConfig.BuildSessionFactory().OpenSession();
                using var transaction = session.BeginTransaction();

                try
                {
                    await exec.Invoke(session);
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    _logger.Error("Error happened during the transaction");
                    session.Clear();
                    await transaction.RollbackAsync(ct);

                    throw;
                }

                session.Close();
            }
            catch (Exception)
            {
                _logger.Error("Error happened during the session operation");
                throw;
            }
        }
    }
}
