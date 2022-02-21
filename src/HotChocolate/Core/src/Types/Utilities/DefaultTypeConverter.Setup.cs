using System;
using System.Collections.Generic;
using System.Globalization;
using SysConv = System.Convert;

namespace HotChocolate.Utilities;

public partial class DefaultTypeConverter
{
    private const string _utcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
    private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

    private static void RegisterConverters(
        DefaultTypeConverter registry)
    {
        RegisterDateTimeConversions(registry);
        RegisterGuidConversions(registry);
        RegisterUriConversions(registry);
        RegisterBooleanConversions(registry);
        RegisterStringConversions(registry);
        RegisterNameStringConversions(registry);

        RegisterByteConversions(registry);
        RegisterSByteConversions(registry);

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
        DefaultTypeConverter registry)
    {
        registry.Register<DateTimeOffset, DateTime>(from => from.UtcDateTime);
        registry.Register<DateTime, DateTimeOffset>(from => from);

        registry.Register<DateTimeOffset, long>(from => from.ToUnixTimeSeconds());
        registry.Register<long, DateTimeOffset>(DateTimeOffset.FromUnixTimeSeconds);

        registry.Register<DateTime, long>(
            from => ((DateTimeOffset)from).ToUnixTimeSeconds());
        registry.Register<long, DateTime>(
            from => DateTimeOffset.FromUnixTimeSeconds(from).UtcDateTime);

        registry.Register<DateTimeOffset, string>(
            from => from.ToString(from.Offset == TimeSpan.Zero
                ? _utcFormat
                : _localFormat, CultureInfo.InvariantCulture));
        registry.Register<DateTime, string>(
            from =>
            {
                var offset = new DateTimeOffset(from);
                return offset.ToString(offset.Offset == TimeSpan.Zero
                    ? _utcFormat
                    : _localFormat, CultureInfo.InvariantCulture);
            });

#if NET6_0_OR_GREATER
        registry.Register<DateOnly, DateTimeOffset>(from => from.ToDateTime(default));
        registry.Register<DateTimeOffset, DateOnly>(from => DateOnly.FromDateTime(from.Date));
        registry.Register<DateOnly, DateTime>(from => from.ToDateTime(default));
        registry.Register<DateTime, DateOnly>(from => DateOnly.FromDateTime(from.Date));
        registry.Register<DateOnly, string>(from => from.ToShortDateString());
        registry.Register<string, DateOnly>(
            from => DateOnly.Parse(from, CultureInfo.InvariantCulture));

        registry.Register<TimeOnly, DateTimeOffset>(from => default(DateOnly).ToDateTime(from));
        registry.Register<DateTimeOffset, TimeOnly>(from => TimeOnly.FromDateTime(from.Date));
        registry.Register<TimeOnly, DateTime>(from => default(DateOnly).ToDateTime(from));
        registry.Register<DateTime, TimeOnly>(from => TimeOnly.FromDateTime(from.Date));
        registry.Register<TimeOnly, TimeSpan>(from => from.ToTimeSpan());
        registry.Register<TimeSpan, TimeOnly>(TimeOnly.FromTimeSpan);
        registry.Register<TimeOnly, string>(from => from.ToShortTimeString());
        registry.Register<string, TimeOnly>(
            from => TimeOnly.Parse(from, CultureInfo.InvariantCulture));
#endif
    }

    private static void RegisterGuidConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<Guid, string>(from => from.ToString("N"));
    }

    private static void RegisterUriConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<Uri, string>(from => from.ToString());
    }

    private static void RegisterBooleanConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<bool, string>(from => from.ToString(CultureInfo.InvariantCulture));
        registry.Register<bool, short>(from => from ? (short)1 : (short)0);
        registry.Register<bool, int>(from => from ? 1 : 0);
        registry.Register<bool, long>(from => from ? (long)1 : 0);
    }

    private static void RegisterStringConversions(
        DefaultTypeConverter registry)
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
        DefaultTypeConverter registry)
    {
        registry.Register<NameString, string>(from => from);
    }

    private static void RegisterByteConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<byte, short>(SysConv.ToInt16);
        registry.Register<byte, int>(SysConv.ToInt32);
        registry.Register<byte, long>(SysConv.ToInt64);
        registry.Register<byte, ushort>(SysConv.ToUInt16);
        registry.Register<byte, uint>(SysConv.ToUInt32);
        registry.Register<byte, ulong>(SysConv.ToUInt64);
        registry.Register<byte, decimal>(SysConv.ToDecimal);
        registry.Register<byte, float>(SysConv.ToSingle);
        registry.Register<byte, double>(SysConv.ToDouble);
        registry.Register<byte, sbyte>(SysConv.ToSByte);
        registry.Register<byte, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterSByteConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<sbyte, short>(SysConv.ToInt16);
        registry.Register<sbyte, int>(SysConv.ToInt32);
        registry.Register<sbyte, long>(SysConv.ToInt64);
        registry.Register<sbyte, ushort>(SysConv.ToUInt16);
        registry.Register<sbyte, uint>(SysConv.ToUInt32);
        registry.Register<sbyte, ulong>(SysConv.ToUInt64);
        registry.Register<sbyte, decimal>(SysConv.ToDecimal);
        registry.Register<sbyte, float>(SysConv.ToSingle);
        registry.Register<sbyte, double>(SysConv.ToDouble);
        registry.Register<sbyte, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterUInt16Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<ushort, byte>(SysConv.ToByte);
        registry.Register<ushort, short>(SysConv.ToInt16);
        registry.Register<ushort, int>(SysConv.ToInt32);
        registry.Register<ushort, long>(SysConv.ToInt64);
        registry.Register<ushort, uint>(SysConv.ToUInt32);
        registry.Register<ushort, ulong>(SysConv.ToUInt64);
        registry.Register<ushort, decimal>(SysConv.ToDecimal);
        registry.Register<ushort, float>(SysConv.ToSingle);
        registry.Register<ushort, double>(SysConv.ToDouble);
        registry.Register<ushort, sbyte>(SysConv.ToSByte);
        registry.Register<ushort, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterUInt32Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<uint, byte>(SysConv.ToByte);
        registry.Register<uint, short>(SysConv.ToInt16);
        registry.Register<uint, int>(SysConv.ToInt32);
        registry.Register<uint, long>(SysConv.ToInt64);
        registry.Register<uint, ushort>(SysConv.ToUInt16);
        registry.Register<uint, ulong>(SysConv.ToUInt64);
        registry.Register<uint, decimal>(SysConv.ToDecimal);
        registry.Register<uint, float>(SysConv.ToSingle);
        registry.Register<uint, double>(SysConv.ToDouble);
        registry.Register<uint, sbyte>(SysConv.ToSByte);
        registry.Register<uint, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterUInt64Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<ulong, byte>(SysConv.ToByte);
        registry.Register<ulong, short>(SysConv.ToInt16);
        registry.Register<ulong, int>(SysConv.ToInt32);
        registry.Register<ulong, long>(SysConv.ToInt64);
        registry.Register<ulong, ushort>(SysConv.ToUInt16);
        registry.Register<ulong, uint>(SysConv.ToUInt32);
        registry.Register<ulong, decimal>(SysConv.ToDecimal);
        registry.Register<ulong, float>(SysConv.ToSingle);
        registry.Register<ulong, double>(SysConv.ToDouble);
        registry.Register<ulong, sbyte>(SysConv.ToSByte);
        registry.Register<ulong, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterInt16Conversions(
       DefaultTypeConverter registry)
    {
        registry.Register<short, byte>(SysConv.ToByte);
        registry.Register<short, int>(SysConv.ToInt32);
        registry.Register<short, long>(SysConv.ToInt64);
        registry.Register<short, ushort>(SysConv.ToUInt16);
        registry.Register<short, uint>(SysConv.ToUInt32);
        registry.Register<short, ulong>(SysConv.ToUInt64);
        registry.Register<short, decimal>(SysConv.ToDecimal);
        registry.Register<short, float>(SysConv.ToSingle);
        registry.Register<short, double>(SysConv.ToDouble);
        registry.Register<short, sbyte>(SysConv.ToSByte);
        registry.Register<short, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterInt32Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<int, byte>(SysConv.ToByte);
        registry.Register<int, short>(SysConv.ToInt16);
        registry.Register<int, long>(SysConv.ToInt64);
        registry.Register<int, ushort>(SysConv.ToUInt16);
        registry.Register<int, uint>(SysConv.ToUInt32);
        registry.Register<int, ulong>(SysConv.ToUInt64);
        registry.Register<int, decimal>(SysConv.ToDecimal);
        registry.Register<int, float>(SysConv.ToSingle);
        registry.Register<int, double>(SysConv.ToDouble);
        registry.Register<int, sbyte>(SysConv.ToSByte);
        registry.Register<int, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterInt64Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<long, byte>(SysConv.ToByte);
        registry.Register<long, short>(SysConv.ToInt16);
        registry.Register<long, int>(SysConv.ToInt32);
        registry.Register<long, ushort>(SysConv.ToUInt16);
        registry.Register<long, uint>(SysConv.ToUInt32);
        registry.Register<long, ulong>(SysConv.ToUInt64);
        registry.Register<long, decimal>(SysConv.ToDecimal);
        registry.Register<long, float>(SysConv.ToSingle);
        registry.Register<long, double>(SysConv.ToDouble);
        registry.Register<long, sbyte>(SysConv.ToSByte);
        registry.Register<long, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterSingleConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<float, byte>(SysConv.ToByte);
        registry.Register<float, short>(SysConv.ToInt16);
        registry.Register<float, int>(SysConv.ToInt32);
        registry.Register<float, long>(SysConv.ToInt64);
        registry.Register<float, ushort>(SysConv.ToUInt16);
        registry.Register<float, uint>(SysConv.ToUInt32);
        registry.Register<float, ulong>(SysConv.ToUInt64);
        registry.Register<float, decimal>(SysConv.ToDecimal);
        registry.Register<float, double>(SysConv.ToDouble);
        registry.Register<float, sbyte>(SysConv.ToSByte);
        registry.Register<float, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterDoubleConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<double, byte>(SysConv.ToByte);
        registry.Register<double, short>(SysConv.ToInt16);
        registry.Register<double, int>(SysConv.ToInt32);
        registry.Register<double, long>(SysConv.ToInt64);
        registry.Register<double, ushort>(SysConv.ToUInt16);
        registry.Register<double, uint>(SysConv.ToUInt32);
        registry.Register<double, ulong>(SysConv.ToUInt64);
        registry.Register<double, decimal>(SysConv.ToDecimal);
        registry.Register<double, float>(SysConv.ToSingle);
        registry.Register<double, sbyte>(SysConv.ToSByte);
        registry.Register<double, string>(from =>
            from.ToString(CultureInfo.InvariantCulture));
    }

    private static void RegisterDecimalConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<decimal, short>(SysConv.ToInt16);
        registry.Register<decimal, int>(SysConv.ToInt32);
        registry.Register<decimal, long>(SysConv.ToInt64);
        registry.Register<decimal, ushort>(SysConv.ToUInt16);
        registry.Register<decimal, uint>(SysConv.ToUInt32);
        registry.Register<decimal, ulong>(SysConv.ToUInt64);
        registry.Register<decimal, float>(SysConv.ToSingle);
        registry.Register<decimal, double>(SysConv.ToDouble);
        registry.Register<decimal, sbyte>(SysConv.ToSByte);
        registry.Register<decimal, string>(from =>
            from.ToString("E", CultureInfo.InvariantCulture));
    }

    private static void RegisterStringListConversions(
        DefaultTypeConverter registry)
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
