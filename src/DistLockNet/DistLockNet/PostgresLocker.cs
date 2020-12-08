using Dapper;
using DistLockNet.Interfaces;
using DistLockNet.Models;
using Npgsql;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DistLockNet
{
    public class PostgresLocker : ILocker
    {
        public LockingObject LockingObject { get; set; }
        public string ConnectionString { get; }
        public int LockTimeOut { get; }
        public int Heartbeat { get; }

        public PostgresLocker(string connectionString, string instanceId, int lockTimeOut = 10, int heartbeat = 2)
        {
            ConnectionString = connectionString;
            LockingObject.InstanceId = instanceId;
            LockTimeOut = lockTimeOut;
            Heartbeat = heartbeat;
        }

        public async Task CreateLock()
        {
            LockingObject.Id = Guid.NewGuid();
            LockingObject.LockId = Guid.NewGuid();

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    var sqlStatement = @"
                            INSERT INTO LockingObjects 
                            (Id
                            ,LockId
                            ,InstanceId)
                            VALUES (@Id
                            ,@LockId
                            ,@InstanceId)";
                    await conn.ExecuteAsync(sqlStatement, LockingObject);
                    await transaction.CommitAsync();
                }
            }
        }

        public async Task<LockingObject> GetLock()
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                return (await conn.QueryAsync<LockingObject>("SELECT * FROM LockingObjects WHERE InstanceId=@ID", new {ID = LockingObject.InstanceId}))
                    .FirstOrDefault();
            }
        }

        public async Task UpdateLock(Guid lockingObjectId)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {

                    const string sqlStatement = "UPDATE LockingObjects SET LockId=@LockId where Id=@ID";

                    await conn.ExecuteAsync(sqlStatement, new {LockId=lockingObjectId, ID=LockingObject.Id});
                    await transaction.CommitAsync();
                }
            }
        }
    }
}