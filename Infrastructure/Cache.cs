using System.Collections.Generic;
using System.Linq;
using Pro4Soft.iErpIntegration.Workers;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public class Cache : AppSettings
    {
        protected override string SettingsFileName { get; } = "cache.json";
        public List<SiteCache> SitesCache { get; set; } = new List<SiteCache>();

        public SiteCache this[string name]
        {
            get
            {
                var existing = SitesCache.SingleOrDefault(c => c.Name == name);
                if (existing != null)
                    return existing;

                existing = new SiteCache
                {
                    Name = name
                };
                SitesCache.Add(existing);
                return existing;
            }
        }
    }
}