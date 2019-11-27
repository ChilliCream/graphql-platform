using System;
using System.Collections.Generic;
using System.Globalization;
using SysConv = System.Convert;

namespace HotChocolate.Utilities
{
    public partial class TypeConversion
    {
        private const string _utcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
        private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

        private static void RegisterConverters(
            ITypeConverterRegistry registry)
        {
            RegisterDateTimeConversions(registry);
            RegisterGuidConversions(registry);
            RegisterUriConversions(registry);
            RegisterBooleanConversions(registry);
            RegisterStringConversions(registry);
            RegisterNameStringConversions(registry);

            RegisterByteConversions(registry);

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

            registry.Register<DateTimeOffset, string>(
                from =>
                {
                    if (from.Offset == TimeSpan.Zero)
                    {
                        return from.ToString(
                            _utcFormat,
                            CultureInfo.InvariantCulture);
                    }

                    return from.ToString(
                        _localFormat,
                        CultureInfo.InvariantCulture);
                });
            registry.Register<DateTime, string>(
                from =>
                {
                    var offset = new DateTimeOffset(from);

                    if (offset.Offset == TimeSpan.Zero)
                    {
                        return offset.ToString(
                            _utcFormat,
                            CultureInfo.InvariantCulture);
                    }

                    return offset.ToString(
                        _localFormat,
                        CultureInfo.InvariantCulture);
                });
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
            registry.Register<string, NameString>(from => from);
        }

        private static void RegisterNameStringConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<NameString, string>(from => from);
        }

        private static void RegisterByteConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<byte, short>(from => SysConv.ToInt16(from));
            registry.Register<byte, int>(from => SysConv.ToInt32(from));
            registry.Register<byte, long>(from => SysConv.ToInt64(from));
            registry.Register<byte, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<byte, uint>(from => SysConv.ToUInt32(from));
            registry.Register<byte, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<byte, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<byte, float>(from => SysConv.ToSingle(from));
            registry.Register<byte, double>(from => SysConv.ToDouble(from));
            registry.Register<byte, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterUInt16Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<ushort, byte>(from => SysConv.ToByte(from));
            registry.Register<ushort, short>(from => SysConv.ToInt16(from));
            registry.Register<ushort, int>(from => SysConv.ToInt32(from));
            registry.Register<ushort, long>(from => SysConv.ToInt64(from));
            registry.Register<ushort, uint>(from => SysConv.ToUInt32(from));
            registry.Register<ushort, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<ushort, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<ushort, float>(from => SysConv.ToSingle(from));
            registry.Register<ushort, double>(from => SysConv.ToDouble(from));
            registry.Register<ushort, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterUInt32Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<uint, byte>(from => SysConv.ToByte(from));
            registry.Register<uint, short>(from => SysConv.ToInt16(from));
            registry.Register<uint, int>(from => SysConv.ToInt32(from));
            registry.Register<uint, long>(from => SysConv.ToInt64(from));
            registry.Register<uint, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<uint, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<uint, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<uint, float>(from => SysConv.ToSingle(from));
            registry.Register<uint, double>(from => SysConv.ToDouble(from));
            registry.Register<uint, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterUInt64Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<ulong, byte>(from => SysConv.ToByte(from));
            registry.Register<ulong, short>(from => SysConv.ToInt16(from));
            registry.Register<ulong, int>(from => SysConv.ToInt32(from));
            registry.Register<ulong, long>(from => SysConv.ToInt64(from));
            registry.Register<ulong, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<ulong, uint>(from => SysConv.ToUInt32(from));
            registry.Register<ulong, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<ulong, float>(from => SysConv.ToSingle(from));
            registry.Register<ulong, double>(from => SysConv.ToDouble(from));
            registry.Register<ulong, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterInt16Conversions(
           ITypeConverterRegistry registry)
        {
            registry.Register<short, byte>(from => SysConv.ToByte(from));
            registry.Register<short, int>(from => SysConv.ToInt32(from));
            registry.Register<short, long>(from => SysConv.ToInt64(from));
            registry.Register<short, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<short, uint>(from => SysConv.ToUInt32(from));
            registry.Register<short, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<short, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<short, float>(from => SysConv.ToSingle(from));
            registry.Register<short, double>(from => SysConv.ToDouble(from));
            registry.Register<short, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterInt32Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<int, byte>(from => SysConv.ToByte(from));
            registry.Register<int, short>(from => SysConv.ToInt16(from));
            registry.Register<int, long>(from => SysConv.ToInt64(from));
            registry.Register<int, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<int, uint>(from => SysConv.ToUInt32(from));
            registry.Register<int, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<int, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<int, float>(from => SysConv.ToSingle(from));
            registry.Register<int, double>(from => SysConv.ToDouble(from));
            registry.Register<int, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterInt64Conversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<long, byte>(from => SysConv.ToByte(from));
            registry.Register<long, short>(from => SysConv.ToInt16(from));
            registry.Register<long, int>(from => SysConv.ToInt32(from));
            registry.Register<long, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<long, uint>(from => SysConv.ToUInt32(from));
            registry.Register<long, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<long, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<long, float>(from => SysConv.ToSingle(from));
            registry.Register<long, double>(from => SysConv.ToDouble(from));
            registry.Register<long, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterSingleConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<float, byte>(from => SysConv.ToByte(from));
            registry.Register<float, short>(from => SysConv.ToInt16(from));
            registry.Register<float, int>(from => SysConv.ToInt32(from));
            registry.Register<float, long>(from => SysConv.ToInt64(from));
            registry.Register<float, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<float, uint>(from => SysConv.ToUInt32(from));
            registry.Register<float, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<float, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<float, double>(from => SysConv.ToDouble(from));
            registry.Register<float, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterDoubleConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<double, byte>(from => SysConv.ToByte(from));
            registry.Register<double, short>(from => SysConv.ToInt16(from));
            registry.Register<double, int>(from => SysConv.ToInt32(from));
            registry.Register<double, long>(from => SysConv.ToInt64(from));
            registry.Register<double, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<double, uint>(from => SysConv.ToUInt32(from));
            registry.Register<double, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<double, decimal>(from => SysConv.ToDecimal(from));
            registry.Register<double, float>(from => SysConv.ToSingle(from));
            registry.Register<double, string>(from =>
                from.ToString(CultureInfo.InvariantCulture));
        }

        private static void RegisterDecimalConversions(
            ITypeConverterRegistry registry)
        {
            registry.Register<decimal, short>(from => SysConv.ToInt16(from));
            registry.Register<decimal, int>(from => SysConv.ToInt32(from));
            registry.Register<decimal, long>(from => SysConv.ToInt64(from));
            registry.Register<decimal, ushort>(from => SysConv.ToUInt16(from));
            registry.Register<decimal, uint>(from => SysConv.ToUInt32(from));
            registry.Register<decimal, ulong>(from => SysConv.ToUInt64(from));
            registry.Register<decimal, float>(from => SysConv.ToSingle(from));
            registry.Register<decimal, double>(from => SysConv.ToDouble(from));
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
