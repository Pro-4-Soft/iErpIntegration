using System.IO;
using Newtonsoft.Json;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public class AppSettings
    {
        protected virtual string SettingsFileName { get; } = "settings.json";

        public T Initialize<T>() where T : AppSettings, new()
        {
            return Utils.DeserializeFromJson<T>(Utils.ReadTextFile(Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName), false), null, false, new T());
        }

        public string Serialize()
        {
            return Utils.SerializeToStringJson(this, Formatting.Indented);
        }

        public void SaveToFile()
        {
            Utils.WriteTextFile(Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName), Serialize());
        }
    }
}
