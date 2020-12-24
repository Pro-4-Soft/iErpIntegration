using System;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public static class Extensions
    {
        public static T ParseEnum<T>(this string value, bool throwExc = true, T defaultVal = default)
        {
            try
            {
                return (T)Enum.Parse(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), value, true);
            }
            catch
            {
                if (throwExc)
                    throw;
                return defaultVal;
            }
        }

        public static long ParseLong(this string value, bool throwExc = true, long defaultVal = default)
        {
            try
            {
                return long.Parse(value);
            }
            catch
            {
                if (throwExc)
                    throw;
                return defaultVal;
            }
        }

        public static int ParseInt(this string value, bool throwExc = true, int defaultVal = default)
        {
            try
            {
                return int.Parse(value);
            }
            catch
            {
                if (throwExc)
                    throw;
                return defaultVal;
            }
        }

        public static double ParseDouble(this string value, bool throwExc = true, double defaultVal = default)
        {
            try
            {
                return double.Parse(value);
            }
            catch
            {
                if (throwExc)
                    throw;
                return defaultVal;
            }
        }

        public static long? ParseLongNullable(this string value)
        {
            return long.TryParse(value, out var result) ? result : (long?)null;
        }

        public static int? ParseIntNullable(this string value)
        {
            return int.TryParse(value, out var result) ? result : (int?)null;
        }

        public static double? ParseDoubleNullable(this string value)
        {
            return double.TryParse(value, out var result) ? result : (double?)null;
        }
    }
}
