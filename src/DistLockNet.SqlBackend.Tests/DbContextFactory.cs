using Microsoft.EntityFrameworkCore;

namespace DistLockNet.SqlBackend.Tests
{
    public class DbContextFactory : IDbContextFactory
    {
        private readonly IDataProviderSelector _dataProviderSelector;

        public DbContextFactory(IDataProviderSelector dataProviderSelector)
        {
            _dataProviderSelector = dataProviderSelector;
        }
        public DbContext GetContext()
        {
            var res = new LockerDbContext(_dataProviderSelector);

            res.Database.EnsureCreated();

            return res;
        }
    }
}
