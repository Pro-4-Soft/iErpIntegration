using System;
using System.Collections.Generic;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public class ScheduleSetting
    {
        public string Name { get; set; }
        public bool Active { get; set; } = true;
        public bool RunOnStartup { get; set; } = false;
        public DateTimeOffset? Start { get; set; }
        public TimeSpan? Sleep { get; set; }
        public string Class { get; set; }
        public string ThreadName { get; set; } = "Default";

        public TimeSpan GetTimeout(DateTimeOffset now)
        {
            if (Sleep == null)
                return TimeSpan.MaxValue;

            if (Start == null)
                Start = now;

            var totalMillis = (long)now.Subtract(Start.Value).TotalMilliseconds;
            var wholeIntervals = totalMillis / (long)Sleep?.TotalMilliseconds;
            var nextRun = Start?.Add(TimeSpan.FromMilliseconds((wholeIntervals + 1) * (long)Sleep?.TotalMilliseconds));
            return nextRun.Value.Subtract(now);
        }

        public Dictionary<string, string> AdditionalSettings { get; set; } = new Dictionary<string, string>();

        public string this[string key] => AdditionalSettings.TryGetValue(key, out var res) ? res : null;
    }
}