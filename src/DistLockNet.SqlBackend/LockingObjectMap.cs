using FluentNHibernate.Mapping;

namespace DistLockNet.SqlBackend
{
    public class LockingObjectMap : ClassMap<LockingObjectEntity>
    {
        public LockingObjectMap()
        {
            Table("distlocks");
            Id(x => x.AppId);
            Map(x => x.LockerId);
            Map(x => x.Seed);
        }
    }
}