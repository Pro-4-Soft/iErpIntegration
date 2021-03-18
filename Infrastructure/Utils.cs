﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public class Utils
    {
        public static T DeserializeFromJson<T>(string str, string root = null, bool throwError = true, T defaultVal = null) where T : class
        {
            try
            {
                var reader = new JsonTextReader(new StringReader(str));
                var serializer = new JsonSerializer { DateParseHandling = DateParseHandling.DateTimeOffset };
                var des = serializer.Deserialize(reader);
                if (!(des is JToken jToken))
                    return des as T;
                if (root != null)
                    jToken = jToken.SelectToken(root);
                return jToken?.ToObject<T>();
            }
            catch
            {
                if (throwError)
                    throw;
                return defaultVal;
            }
        }

        public static string SerializeToStringJson(object obj, Formatting formatting = Formatting.None, bool throwError = true)
        {
            try
            {
                if (obj == null)
                    return null;
                return JsonConvert.SerializeObject(obj, formatting, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            }
            catch
            {
                if (throwError)
                    throw;
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void WriteTextFile(string fileName, string data)
        {
            FileInfo file = new FileInfo(fileName);
            if (file.Directory != null && !file.Directory.Exists)
                file.Directory.Create();
            using (var writer = new StreamWriter(fileName))
            {
                writer.WriteLine(data);
                writer.Flush();
                writer.Close();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string ReadTextFile(string fileName, bool throwException = true, string defaultValue = default)
        {
            try
            {
                using (var reader = new StreamReader(File.Open(fileName, FileMode.Open, FileAccess.Read)))
                    return reader.ReadToEnd();
            }
            catch
            {
                if (throwException)
                    throw;
                return defaultValue;
            }
        }

        public static DateTime Time(DateTime? time = null, string timeZone = "Eastern Standard Time")
        {
            return TimeZoneInfo.ConvertTimeFromUtc(time ?? DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
        }
    }
}
