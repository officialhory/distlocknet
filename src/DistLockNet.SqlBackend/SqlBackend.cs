using DistLockNet.Interfaces;
using DistLockNet.Models;
using DistLockNet.SqlBackend.Exception;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DistLockNet.SqlBackend
{
    public class SqlBackend : ILockingBnd
    {
        private readonly ILogger _logger;
        private readonly IDbContextFactory _contextFactory;

        public SqlBackend(IDbContextFactory contextFactory, ILogger logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<LockingObject> GetAsync(string application, CancellationToken ct)
        {
            _logger.Verbose("Trying to get LockingObject for application: {Application}", application);
            try
            {
                LockingObjectEntity loe = null;
                await ExecuteTransactionAsync(async context =>
                {
                    loe = await context.Set<LockingObjectEntity>().Where(i => i.AppId == application).FirstOrDefaultAsync(ct);
                }, ct);

                _logger.Verbose("Successfully get LockingObject for application: {Application}", application);
                return loe == null ? null : new LockingObject(loe.AppId, loe.LockerId, loe.Seed);
            }
            catch (System.Exception e)
            {
                _logger.Verbose(e, "Error during getting LockingObject.");
                return null;
            }

        }

        public async Task<bool> AddAsync(LockingObject lo, CancellationToken ct)
        {
            _logger.Verbose("Trying to add LockingObject for application: {Application}", lo.AppId);
            try
            {
                await ExecuteTransactionAsync(async context =>
                {
                    await context.AddAsync(new LockingObjectEntity
                    {
                        AppId = lo.AppId,
                        LockerId = lo.LockerId,
                        Seed = lo.Seed
                    }, ct);

                }, ct);

                _logger.Verbose("Successfully add LockingObject for application: {Application}", lo.AppId);
                return true;
            }
            catch (System.Exception e)
            {
                _logger.Verbose(e, "Error during adding LockingObject.");
                return false;
            }
        }

        public async Task<bool> AllocateAsync(LockingObject lo, CancellationToken ct)
        {
            _logger.Verbose("Trying to allocate LockingObject for application: {Application}", lo.AppId);
            return await ModifyAsync(lo, x => x.AppId == lo.AppId, ct);
        }

        public async Task<bool> UpdateAsync(LockingObject lo, CancellationToken ct)
        {
            _logger.Verbose("Trying to update LockingObject for application: {Application}", lo.AppId);
            return await ModifyAsync(lo, x => x.AppId == lo.AppId && x.LockerId == lo.LockerId, ct);
        }

        private async Task<bool> ModifyAsync(LockingObject lo,
            Expression<Func<LockingObjectEntity, bool>> predicate, CancellationToken ct)
        {
            try
            {
                await ExecuteTransactionAsync(async context =>
                {
                    var loe = await context.Set<LockingObjectEntity>().Where(predicate).FirstOrDefaultAsync(ct);
                    if (loe == null)
                    {
                        throw new SqlBackendException($"LockingObjectEntity does not exist for application: {lo.AppId}, with LockerId: {lo.LockerId}");
                    }

                    loe.LockerId = lo.LockerId;
                    loe.Seed = lo.Seed;
                }, ct);

                _logger.Verbose("Successfully modify LockingObject for application: {Application}", lo.AppId);
                return true;
            }
            catch (System.Exception e)
            {
                _logger.Verbose(e, "Error during updating LockingObject.");
                return false;
            }
        }


        private async Task ExecuteTransactionAsync(Func<DbContext, Task> exec, CancellationToken ct)
        {
            try
            {
                using (var context = _contextFactory.GetContext())
                {
                    using (var transaction = await context.Database.BeginTransactionAsync(ct))
                    {
                        try
                        {
                            await exec.Invoke(context);
                            await context.SaveChangesAsync(ct);
                            await transaction.CommitAsync(ct);
                        }
                        catch (System.Exception e)
                        {
                            _logger.Error($"Error happened during the transaction: {e}");
                            await transaction.RollbackAsync(ct);
                            throw;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                _logger.Error($"Error happened during the context operation: {e}");
                throw;
            }
        }
    }
}
