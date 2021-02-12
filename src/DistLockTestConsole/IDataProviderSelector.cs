using Microsoft.EntityFrameworkCore;

namespace DistLockTestConsole
{
    public interface IDataProviderSelector
    {
        void UseSql(DbContextOptionsBuilder optionsBuilder);
    }
}
