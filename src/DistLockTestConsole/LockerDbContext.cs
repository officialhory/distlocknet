using DistLockNet.SqlBackend;
using Microsoft.EntityFrameworkCore;

namespace DistLockTestConsole
{
    public class LockerDbContext : DbContext
    {
        private readonly IDataProviderSelector _dataProviderSelector;

        public DbSet<LockingObjectEntity> LockingObjects { get; set; }

        public LockerDbContext(IDataProviderSelector dataProviderSelector)
        {
            _dataProviderSelector = dataProviderSelector;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _dataProviderSelector.UseSql(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LockingObjectEntity>();
        }
    }
}
