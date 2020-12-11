using FluentNHibernate.Mapping;

namespace DistLockNet.SqlBackend
{
    public class LockingObjectMap : ClassMap<LockingObjectEntity>
    {
        public LockingObjectMap()
        {
            Table("distlocks");
            Version(x => x.Version);
            Id(x => x.AppId);
            Map(x => x.LockerId);
            Map(x => x.Seed);
        }
    }
}