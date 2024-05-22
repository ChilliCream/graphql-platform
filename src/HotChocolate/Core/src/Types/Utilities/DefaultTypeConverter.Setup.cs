#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;
using HotChocolate.Language;
using static System.DateTimeOffset;
using static System.Globalization.CultureInfo;
using static System.Globalization.NumberStyles;
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
        registry.Register<DateTime, DateTimeOffset>(
            from =>
            {
                if (from.Kind is DateTimeKind.Unspecified)
                {
                    from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
                }
                
                return new DateTimeOffset(from);
            });

        registry.Register<DateTimeOffset, long>(from => from.ToUnixTimeSeconds());
        registry.Register<long, DateTimeOffset>(from => FromUnixTimeSeconds(from));

        registry.Register<DateTime, long>(from => ((DateTimeOffset)from).ToUnixTimeSeconds());
        registry.Register<long, DateTime>(from => FromUnixTimeSeconds(from).UtcDateTime);

        registry.Register<DateTimeOffset, string>(
            from => from.ToString(from.Offset == TimeSpan.Zero
                ? _utcFormat
                : _localFormat, InvariantCulture));
        registry.Register<DateTime, string>(
            from =>
            {
                var offset = new DateTimeOffset(from);
                return offset.ToString(offset.Offset == TimeSpan.Zero
                    ? _utcFormat
                    : _localFormat, InvariantCulture);
            });

#if NET6_0_OR_GREATER
        registry.Register<DateOnly, DateTimeOffset>(from => from.ToDateTime(default));
        registry.Register<DateTimeOffset, DateOnly>(from => DateOnly.FromDateTime(from.Date));
        registry.Register<DateOnly, DateTime>(from => from.ToDateTime(default));
        registry.Register<DateTime, DateOnly>(from => DateOnly.FromDateTime(from.Date));
        registry.Register<DateOnly, string>(from => from.ToShortDateString());
        registry.Register<string, DateOnly>(from => DateOnly.Parse(from, InvariantCulture));

        registry.Register<TimeOnly, DateTimeOffset>(from => default(DateOnly).ToDateTime(from));
        registry.Register<DateTimeOffset, TimeOnly>(from => TimeOnly.FromDateTime(from.Date));
        registry.Register<TimeOnly, DateTime>(from => default(DateOnly).ToDateTime(from));
        registry.Register<DateTime, TimeOnly>(from => TimeOnly.FromDateTime(from.Date));
        registry.Register<TimeOnly, TimeSpan>(from => from.ToTimeSpan());
        registry.Register<TimeSpan, TimeOnly>(from => TimeOnly.FromTimeSpan(from));
        registry.Register<TimeOnly, string>(from => from.ToShortTimeString());
        registry.Register<string, TimeOnly>(from => TimeOnly.Parse(from, InvariantCulture));
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
        registry.Register<bool, string>(from => from.ToString(InvariantCulture));
        registry.Register<bool, short>(from => from ? (short)1 : (short)0);
        registry.Register<bool, int>(from => from ? 1 : 0);
        registry.Register<bool, long>(from => from ? (long)1 : 0);
    }

    private static void RegisterStringConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<string, Guid>(
            static from => Guid.Parse(from));
        registry.Register<string, Uri>(
            static from => new Uri(from, UriKind.RelativeOrAbsolute));
        registry.Register<string, short>(
            static from => short.Parse(from, Integer, InvariantCulture));
        registry.Register<string, int>(
            static from => int.Parse(from, Integer, InvariantCulture));
        registry.Register<string, long>(
            static from => long.Parse(from, Integer, InvariantCulture));
        registry.Register<string, ushort>(
            static from => ushort.Parse(from, Integer, InvariantCulture));
        registry.Register<string, uint>(
            static from => uint.Parse(from, Integer, InvariantCulture));
        registry.Register<string, ulong>(
            static from => ulong.Parse(from, Integer, InvariantCulture));
        registry.Register<string, float>(
            static from => ushort.Parse(from, Float, InvariantCulture));
        registry.Register<string, double>(
            static from => double.Parse(from, Float, InvariantCulture));
        registry.Register<string, decimal>(
            static from => decimal.Parse(from, Float, InvariantCulture));
        registry.Register<string, bool>(
            static from => bool.Parse(from));
        registry.Register<string, JsonElement>(
            static from =>
            {
                var length = checked(from.Length * 4);
                byte[]? jsonText = null;

                var jsonTextSpan = length <= GraphQLConstants.StackallocThreshold
                    ? stackalloc byte[length]
                    : jsonText = ArrayPool<byte>.Shared.Rent(length);

                Utf8GraphQLParser.ConvertToBytes(from, ref jsonTextSpan);
                var jsonReader = new Utf8JsonReader(jsonTextSpan);
                var element = JsonElement.ParseValue(ref jsonReader);

                if (jsonText is not null)
                {
                    ArrayPool<byte>.Shared.Return(jsonText);
                }

                return element;
            });
        registry.Register<JsonElement, string>(static from => from.ToString());
    }

    private static void RegisterByteConversions(
        DefaultTypeConverter registry)
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
        registry.Register<byte, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<byte, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterSByteConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<sbyte, short>(from => SysConv.ToInt16(from));
        registry.Register<sbyte, int>(from => SysConv.ToInt32(from));
        registry.Register<sbyte, long>(from => SysConv.ToInt64(from));
        registry.Register<sbyte, ushort>(from => SysConv.ToUInt16(from));
        registry.Register<sbyte, uint>(from => SysConv.ToUInt32(from));
        registry.Register<sbyte, ulong>(from => SysConv.ToUInt64(from));
        registry.Register<sbyte, decimal>(from => SysConv.ToDecimal(from));
        registry.Register<sbyte, float>(from => SysConv.ToSingle(from));
        registry.Register<sbyte, double>(from => SysConv.ToDouble(from));
        registry.Register<sbyte, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterUInt16Conversions(
        DefaultTypeConverter registry)
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
        registry.Register<ushort, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<ushort, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterUInt32Conversions(
        DefaultTypeConverter registry)
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
        registry.Register<uint, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<uint, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterUInt64Conversions(
        DefaultTypeConverter registry)
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
        registry.Register<ulong, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<ulong, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterInt16Conversions(
       DefaultTypeConverter registry)
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
        registry.Register<short, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<short, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterInt32Conversions(
        DefaultTypeConverter registry)
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
        registry.Register<int, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<int, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterInt64Conversions(
        DefaultTypeConverter registry)
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
        registry.Register<long, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<long, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterSingleConversions(
        DefaultTypeConverter registry)
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
        registry.Register<float, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<float, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterDoubleConversions(
        DefaultTypeConverter registry)
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
        registry.Register<double, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<double, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterDecimalConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<decimal, short>(from => SysConv.ToInt16(from));
        registry.Register<decimal, int>(from => SysConv.ToInt32(from));
        registry.Register<decimal, long>(from => SysConv.ToInt64(from));
        registry.Register<decimal, ushort>(from => SysConv.ToUInt16(from));
        registry.Register<decimal, uint>(from => SysConv.ToUInt32(from));
        registry.Register<decimal, ulong>(from => SysConv.ToUInt64(from));
        registry.Register<decimal, float>(from => SysConv.ToSingle(from));
        registry.Register<decimal, double>(from => SysConv.ToDouble(from));
        registry.Register<decimal, sbyte>(from => SysConv.ToSByte(from));
        registry.Register<decimal, string>(from => from.ToString("E", InvariantCulture));
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
