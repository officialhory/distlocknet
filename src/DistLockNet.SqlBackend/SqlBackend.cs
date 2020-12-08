using DistLockNet.Interfaces;
using DistLockNet.Models;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.Extensions.Configuration;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet.SqlBackend
{
    public class SqlBackend: ILockingBnd
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

            // return null if any issue
            try
            {
                await ExecuteTransactionAsync(async session =>
                {
                    var qq = (await session.Query<LockingObjectEntity>().ToListAsync(ct)).FirstOrDefault();
                    return qq;
                }, ct);
            }
            catch
            {
                return null;
            }
        }

        public Task<bool> AddAsync(LockingObject lo, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct)
        {
            throw new NotImplementedException();
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

        private async Task ExecuteTransactionAsync<TReturn>(Func<Task<TReturn>> exec, CancellationToken ct)
        {
            try
            {
                var dbConfig = DatabaseConfiguration(_databaseType);
                var session = dbConfig.BuildSessionFactory().OpenSession();
                using var transaction = session.BeginTransaction();

                try
                {

                    //TODO Fix this chaos
                    var res = await exec.Invoke();
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    _logger.Error("Error happened during the transaction");
                    session.Clear();
                    await transaction.RollbackAsync(ct);
                }

                session.Close();
            }
            catch (Exception)
            {
                _logger.Error("Error happened during the session operation");
            }

        }
    }
}
