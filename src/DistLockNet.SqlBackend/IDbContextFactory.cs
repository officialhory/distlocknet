using Microsoft.EntityFrameworkCore;

namespace DistLockNet.SqlBackend
{
    public interface IDbContextFactory
    {
        DbContext GetContext();
    }
}
