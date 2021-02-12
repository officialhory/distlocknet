using Microsoft.EntityFrameworkCore;

namespace DistLockNet.SqlBackend.Tests
{
    public interface IDataProviderSelector
    {
        void UseSql(DbContextOptionsBuilder optionsBuilder);
    }
}
