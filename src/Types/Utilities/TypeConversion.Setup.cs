using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace HotChocolate.Utilities
{
    public partial class TypeConversion
    {
        private static void RegisterConverters(
            ITypeConverterRegistry registry)
        {
            RegisterDateTimeConversions(registry);
            RegisterGuidConversions(registry);
            RegisterUriConversions(registry);
            RegisterBooleanConversions(registry);
            RegisterStringConversions(registry);

            RegisterUInt16Conversions(registry);
            RegisterUInt32Conversions(registry);
            RegisterUInt64Conversions(registry);

            RegisterInt16Conversions(registry);
            RegisterInt32Conversions(registry);
            RegisterInt64Conversions(registry);

            RegisterSingleConversions(registry);
            RegisterDoubleConversions(registry);
            RegisterDecimalConversions(registry);

            RegisterStringListConversions(registry);
        }

        private static void RegisterDateTimeConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<DateTimeOffset, DateTime>(
                from => from.UtcDateTime);
            registry.Register<DateTime, DateTimeOffset>(
                from => (DateTimeOffset)from);

            registry.Register<DateTimeOffset, long>(
                from => from.ToUnixTimeSeconds());
            registry.Register<long, DateTimeOffset>(
                from => DateTimeOffset.FromUnixTimeSeconds(from));

            registry.Register<DateTime, long>(
                from => ((DateTimeOffset)from).ToUnixTimeSeconds());
            registry.Register<long, DateTime>(
                from => DateTimeOffset.FromUnixTimeSeconds(from).UtcDateTime);
        }

        private static void RegisterGuidConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<Guid, string>(from => from.ToString("N"));
        }

        private static void RegisterUriConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<Uri, string>(from => from.ToString());
        }

        private static void RegisterBooleanConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<bool, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
            registry.Register<bool, short>(from =>
                from ? (short)1 : (short)0);
            registry.Register<bool, int>(from =>
                from ? (int)1 : (int)0);
            registry.Register<bool, long>(from =>
                from ? (long)1 : (long)0);
        }

        private static void RegisterStringConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<string, Guid>(from => Guid.Parse(from));
            registry.Register<string, Uri>(from =>
                new Uri(from, UriKind.RelativeOrAbsolute));
            registry.Register<string, short>(from =>
                short.Parse(from, NumberStyles.Integer,
                    CultureInfo.InvariantCulture));
            registry.Register<string, int>(from =>
                int.Parse(from, NumberStyles.Integer,
                    CultureInfo.InvariantCulture));
            registry.Register<string, long>(from =>
                long.Parse(from, NumberStyles.Integer,
                    CultureInfo.InvariantCulture));
            registry.Register<string, ushort>(from =>
                ushort.Parse(from, NumberStyles.Integer,
                    CultureInfo.InvariantCulture));
            registry.Register<string, uint>(from =>
                uint.Parse(from, NumberStyles.Integer,
                    CultureInfo.InvariantCulture));
            registry.Register<string, ulong>(from =>
                ulong.Parse(from, NumberStyles.Integer,
                    CultureInfo.InvariantCulture));
            registry.Register<string, float>(from =>
                ushort.Parse(from, NumberStyles.Float,
                    CultureInfo.InvariantCulture));
            registry.Register<string, double>(from =>
                double.Parse(from, NumberStyles.Float,
                    CultureInfo.InvariantCulture));
            registry.Register<string, decimal>(from =>
                decimal.Parse(from, NumberStyles.Float,
                    CultureInfo.InvariantCulture));
            registry.Register<string, bool>(from =>
                bool.Parse(from));
        }

        private static void RegisterUInt16Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<ushort, short>(from => Convert.ToInt16(from));
            registry.Register<ushort, int>(from => Convert.ToInt32(from));
            registry.Register<ushort, long>(from => Convert.ToInt64(from));
            registry.Register<ushort, uint>(from => Convert.ToUInt32(from));
            registry.Register<ushort, ulong>(from => Convert.ToUInt64(from));
            registry.Register<ushort, decimal>(from => Convert.ToDecimal(from));
            registry.Register<ushort, float>(from => Convert.ToSingle(from));
            registry.Register<ushort, double>(from => Convert.ToDouble(from));
            registry.Register<ushort, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterUInt32Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<uint, short>(from => Convert.ToInt16(from));
            registry.Register<uint, int>(from => Convert.ToInt32(from));
            registry.Register<uint, long>(from => Convert.ToInt64(from));
            registry.Register<uint, ushort>(from => Convert.ToUInt16(from));
            registry.Register<uint, ulong>(from => Convert.ToUInt64(from));
            registry.Register<uint, decimal>(from => Convert.ToDecimal(from));
            registry.Register<uint, float>(from => Convert.ToSingle(from));
            registry.Register<uint, double>(from => Convert.ToDouble(from));
            registry.Register<uint, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterUInt64Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<ulong, short>(from => Convert.ToInt16(from));
            registry.Register<ulong, int>(from => Convert.ToInt32(from));
            registry.Register<ulong, long>(from => Convert.ToInt64(from));
            registry.Register<ulong, ushort>(from => Convert.ToUInt16(from));
            registry.Register<ulong, uint>(from => Convert.ToUInt32(from));
            registry.Register<ulong, decimal>(from => Convert.ToDecimal(from));
            registry.Register<ulong, float>(from => Convert.ToSingle(from));
            registry.Register<ulong, double>(from => Convert.ToDouble(from));
            registry.Register<ulong, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterInt16Conversions(
           ITypeConverterRegistry registry)
        {
            registry.Register<short, int>(from => Convert.ToInt32(from));
            registry.Register<short, long>(from => Convert.ToInt64(from));
            registry.Register<short, ushort>(from => Convert.ToUInt16(from));
            registry.Register<short, uint>(from => Convert.ToUInt32(from));
            registry.Register<short, ulong>(from => Convert.ToUInt64(from));
            registry.Register<short, decimal>(from => Convert.ToDecimal(from));
            registry.Register<short, float>(from => Convert.ToSingle(from));
            registry.Register<short, double>(from => Convert.ToDouble(from));
            registry.Register<short, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterInt32Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<int, short>(from => Convert.ToInt16(from));
            registry.Register<int, long>(from => Convert.ToInt64(from));
            registry.Register<int, ushort>(from => Convert.ToUInt16(from));
            registry.Register<int, uint>(from => Convert.ToUInt32(from));
            registry.Register<int, ulong>(from => Convert.ToUInt64(from));
            registry.Register<int, decimal>(from => Convert.ToDecimal(from));
            registry.Register<int, float>(from => Convert.ToSingle(from));
            registry.Register<int, double>(from => Convert.ToDouble(from));
            registry.Register<int, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterInt64Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<long, short>(from => Convert.ToInt16(from));
            registry.Register<long, int>(from => Convert.ToInt32(from));
            registry.Register<long, ushort>(from => Convert.ToUInt16(from));
            registry.Register<long, uint>(from => Convert.ToUInt32(from));
            registry.Register<long, ulong>(from => Convert.ToUInt64(from));
            registry.Register<long, decimal>(from => Convert.ToDecimal(from));
            registry.Register<long, float>(from => Convert.ToSingle(from));
            registry.Register<long, double>(from => Convert.ToDouble(from));
            registry.Register<long, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterSingleConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<float, short>(from => Convert.ToInt16(from));
            registry.Register<float, int>(from => Convert.ToInt32(from));
            registry.Register<float, long>(from => Convert.ToInt64(from));
            registry.Register<float, ushort>(from => Convert.ToUInt16(from));
            registry.Register<float, uint>(from => Convert.ToUInt32(from));
            registry.Register<float, ulong>(from => Convert.ToUInt64(from));
            registry.Register<float, decimal>(from => Convert.ToDecimal(from));
            registry.Register<float, double>(from => Convert.ToDouble(from));
            registry.Register<float, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterDoubleConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<double, short>(from => Convert.ToInt16(from));
            registry.Register<double, int>(from => Convert.ToInt32(from));
            registry.Register<double, long>(from => Convert.ToInt64(from));
            registry.Register<double, ushort>(from => Convert.ToUInt16(from));
            registry.Register<double, uint>(from => Convert.ToUInt32(from));
            registry.Register<double, ulong>(from => Convert.ToUInt64(from));
            registry.Register<double, decimal>(from => Convert.ToDecimal(from));
            registry.Register<double, float>(from => Convert.ToSingle(from));
            registry.Register<double, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterDecimalConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<decimal, short>(from => Convert.ToInt16(from));
            registry.Register<decimal, int>(from => Convert.ToInt32(from));
            registry.Register<decimal, long>(from => Convert.ToInt64(from));
            registry.Register<decimal, ushort>(from => Convert.ToUInt16(from));
            registry.Register<decimal, uint>(from => Convert.ToUInt32(from));
            registry.Register<decimal, ulong>(from => Convert.ToUInt64(from));
            registry.Register<decimal, float>(from => Convert.ToSingle(from));
            registry.Register<decimal, double>(from => Convert.ToDouble(from));
            registry.Register<decimal, string>(from =>
                from.ToString("E", CultureInfo.InvariantCulture));
        }

        private static void RegisterStringListConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<IEnumerable<string>, string>(
                from => string.Join(",", from));

            registry.Register<IReadOnlyCollection<string>, string>(
                from => string.Join(",", from));

            registry.Register<IReadOnlyList<string>, string>(
                from => string.Join(",", from));

            registry.Register<ICollection<string>, string>(
                from => string.Join(",", from));

            registry.Register<IList<string>, string>(
                from => string.Join(",", from));

            registry.Register<string[], string>(
                from => string.Join(",", from));

            registry.Register<List<string>, string>(
                from => string.Join(",", from));
        }
    }
}
