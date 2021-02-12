using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DistLockTestConsole
{
    public class DataProviderSelector : IDataProviderSelector
    {
        private readonly IConfiguration _configuration;

        public DataProviderSelector(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void UseSql(DbContextOptionsBuilder optionsBuilder)
        {
            var cstr = _configuration["Locker:ConnectionString"];

            optionsBuilder.UseNpgsql(cstr);
        }
    }
}
