using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DistLockNet.SqlBackend.Tests
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
            optionsBuilder.UseSqlite($"Data Source=alma");
        }
    }
}
