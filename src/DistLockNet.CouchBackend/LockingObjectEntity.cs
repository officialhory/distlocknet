using Newtonsoft.Json;
using System;

namespace DistLockNet.CouchBackend
{
    public class LockingObjectEntity
    {
        public string AppId { get; set; }
        public Guid LockerId { get; set; }
        public Guid Seed { get; set; }
        [JsonProperty("_rev")]
        public string Revision { get; set; }
    }
}