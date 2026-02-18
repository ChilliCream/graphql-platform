using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Language;
using static System.DateTimeOffset;
using static System.Globalization.CultureInfo;
using static System.Globalization.NumberStyles;
using SysConvert = System.Convert;

namespace HotChocolate.Utilities;

public partial class DefaultTypeConverter
{
    private const string UtcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
    private const string LocalFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

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
        registry.Register<long, DateTimeOffset>(FromUnixTimeSeconds);

        registry.Register<DateTime, long>(from => ((DateTimeOffset)from).ToUnixTimeSeconds());
        registry.Register<long, DateTime>(from => FromUnixTimeSeconds(from).UtcDateTime);

        registry.Register<DateTimeOffset, string>(
            from => from.ToString(from.Offset == TimeSpan.Zero
                ? UtcFormat
                : LocalFormat, InvariantCulture));
        registry.Register<DateTime, string>(
            from =>
            {
                var offset = new DateTimeOffset(from);
                return offset.ToString(offset.Offset == TimeSpan.Zero
                    ? UtcFormat
                    : LocalFormat, InvariantCulture);
            });

        registry.Register<DateOnly, DateTimeOffset>(from => from.ToDateTime(default));
        registry.Register<DateTimeOffset, DateOnly>(from => DateOnly.FromDateTime(from.Date));
        registry.Register<DateOnly, DateTime>(from => from.ToDateTime(default));
        registry.Register<DateTime, DateOnly>(DateOnly.FromDateTime);
        registry.Register<DateOnly, string>(from => from.ToShortDateString());
        registry.Register<string, DateOnly>(from => DateOnly.Parse(from, InvariantCulture));

        registry.Register<TimeOnly, DateTimeOffset>(from => default(DateOnly).ToDateTime(from));
        registry.Register<DateTimeOffset, TimeOnly>(from => TimeOnly.FromDateTime(from.DateTime));
        registry.Register<TimeOnly, DateTime>(from => default(DateOnly).ToDateTime(from));
        registry.Register<DateTime, TimeOnly>(TimeOnly.FromDateTime);
        registry.Register<TimeOnly, TimeSpan>(from => from.ToTimeSpan());
        registry.Register<TimeSpan, TimeOnly>(TimeOnly.FromTimeSpan);
        registry.Register<TimeOnly, string>(from => from.ToShortTimeString());
        registry.Register<string, TimeOnly>(from => TimeOnly.Parse(from, InvariantCulture));
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
        registry.Register<bool, long>(from => from ? 1 : 0);
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

                var jsonTextSpan = length <= GraphQLCharacters.StackallocThreshold
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
        registry.Register<byte, short>(SysConvert.ToInt16);
        registry.Register<byte, int>(SysConvert.ToInt32);
        registry.Register<byte, long>(SysConvert.ToInt64);
        registry.Register<byte, ushort>(SysConvert.ToUInt16);
        registry.Register<byte, uint>(SysConvert.ToUInt32);
        registry.Register<byte, ulong>(SysConvert.ToUInt64);
        registry.Register<byte, decimal>(SysConvert.ToDecimal);
        registry.Register<byte, float>(SysConvert.ToSingle);
        registry.Register<byte, double>(SysConvert.ToDouble);
        registry.Register<byte, sbyte>(SysConvert.ToSByte);
        registry.Register<byte, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterSByteConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<sbyte, short>(SysConvert.ToInt16);
        registry.Register<sbyte, int>(SysConvert.ToInt32);
        registry.Register<sbyte, long>(SysConvert.ToInt64);
        registry.Register<sbyte, ushort>(SysConvert.ToUInt16);
        registry.Register<sbyte, uint>(SysConvert.ToUInt32);
        registry.Register<sbyte, ulong>(SysConvert.ToUInt64);
        registry.Register<sbyte, decimal>(SysConvert.ToDecimal);
        registry.Register<sbyte, float>(SysConvert.ToSingle);
        registry.Register<sbyte, double>(SysConvert.ToDouble);
        registry.Register<sbyte, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterUInt16Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<ushort, byte>(SysConvert.ToByte);
        registry.Register<ushort, short>(SysConvert.ToInt16);
        registry.Register<ushort, int>(SysConvert.ToInt32);
        registry.Register<ushort, long>(SysConvert.ToInt64);
        registry.Register<ushort, uint>(SysConvert.ToUInt32);
        registry.Register<ushort, ulong>(SysConvert.ToUInt64);
        registry.Register<ushort, decimal>(SysConvert.ToDecimal);
        registry.Register<ushort, float>(SysConvert.ToSingle);
        registry.Register<ushort, double>(SysConvert.ToDouble);
        registry.Register<ushort, sbyte>(SysConvert.ToSByte);
        registry.Register<ushort, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterUInt32Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<uint, byte>(SysConvert.ToByte);
        registry.Register<uint, short>(SysConvert.ToInt16);
        registry.Register<uint, int>(SysConvert.ToInt32);
        registry.Register<uint, long>(SysConvert.ToInt64);
        registry.Register<uint, ushort>(SysConvert.ToUInt16);
        registry.Register<uint, ulong>(SysConvert.ToUInt64);
        registry.Register<uint, decimal>(SysConvert.ToDecimal);
        registry.Register<uint, float>(SysConvert.ToSingle);
        registry.Register<uint, double>(SysConvert.ToDouble);
        registry.Register<uint, sbyte>(SysConvert.ToSByte);
        registry.Register<uint, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterUInt64Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<ulong, byte>(SysConvert.ToByte);
        registry.Register<ulong, short>(SysConvert.ToInt16);
        registry.Register<ulong, int>(SysConvert.ToInt32);
        registry.Register<ulong, long>(SysConvert.ToInt64);
        registry.Register<ulong, ushort>(SysConvert.ToUInt16);
        registry.Register<ulong, uint>(SysConvert.ToUInt32);
        registry.Register<ulong, decimal>(SysConvert.ToDecimal);
        registry.Register<ulong, float>(SysConvert.ToSingle);
        registry.Register<ulong, double>(SysConvert.ToDouble);
        registry.Register<ulong, sbyte>(SysConvert.ToSByte);
        registry.Register<ulong, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterInt16Conversions(
       DefaultTypeConverter registry)
    {
        registry.Register<short, byte>(SysConvert.ToByte);
        registry.Register<short, int>(SysConvert.ToInt32);
        registry.Register<short, long>(SysConvert.ToInt64);
        registry.Register<short, ushort>(SysConvert.ToUInt16);
        registry.Register<short, uint>(SysConvert.ToUInt32);
        registry.Register<short, ulong>(SysConvert.ToUInt64);
        registry.Register<short, decimal>(SysConvert.ToDecimal);
        registry.Register<short, float>(SysConvert.ToSingle);
        registry.Register<short, double>(SysConvert.ToDouble);
        registry.Register<short, sbyte>(SysConvert.ToSByte);
        registry.Register<short, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterInt32Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<int, byte>(SysConvert.ToByte);
        registry.Register<int, short>(SysConvert.ToInt16);
        registry.Register<int, long>(SysConvert.ToInt64);
        registry.Register<int, ushort>(SysConvert.ToUInt16);
        registry.Register<int, uint>(SysConvert.ToUInt32);
        registry.Register<int, ulong>(SysConvert.ToUInt64);
        registry.Register<int, decimal>(SysConvert.ToDecimal);
        registry.Register<int, float>(SysConvert.ToSingle);
        registry.Register<int, double>(SysConvert.ToDouble);
        registry.Register<int, sbyte>(SysConvert.ToSByte);
        registry.Register<int, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterInt64Conversions(
        DefaultTypeConverter registry)
    {
        registry.Register<long, byte>(SysConvert.ToByte);
        registry.Register<long, short>(SysConvert.ToInt16);
        registry.Register<long, int>(SysConvert.ToInt32);
        registry.Register<long, ushort>(SysConvert.ToUInt16);
        registry.Register<long, uint>(SysConvert.ToUInt32);
        registry.Register<long, ulong>(SysConvert.ToUInt64);
        registry.Register<long, decimal>(SysConvert.ToDecimal);
        registry.Register<long, float>(SysConvert.ToSingle);
        registry.Register<long, double>(SysConvert.ToDouble);
        registry.Register<long, sbyte>(SysConvert.ToSByte);
        registry.Register<long, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterSingleConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<float, byte>(SysConvert.ToByte);
        registry.Register<float, short>(SysConvert.ToInt16);
        registry.Register<float, int>(SysConvert.ToInt32);
        registry.Register<float, long>(SysConvert.ToInt64);
        registry.Register<float, ushort>(SysConvert.ToUInt16);
        registry.Register<float, uint>(SysConvert.ToUInt32);
        registry.Register<float, ulong>(SysConvert.ToUInt64);
        registry.Register<float, decimal>(SysConvert.ToDecimal);
        registry.Register<float, double>(SysConvert.ToDouble);
        registry.Register<float, sbyte>(SysConvert.ToSByte);
        registry.Register<float, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterDoubleConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<double, byte>(SysConvert.ToByte);
        registry.Register<double, short>(SysConvert.ToInt16);
        registry.Register<double, int>(SysConvert.ToInt32);
        registry.Register<double, long>(SysConvert.ToInt64);
        registry.Register<double, ushort>(SysConvert.ToUInt16);
        registry.Register<double, uint>(SysConvert.ToUInt32);
        registry.Register<double, ulong>(SysConvert.ToUInt64);
        registry.Register<double, decimal>(SysConvert.ToDecimal);
        registry.Register<double, float>(SysConvert.ToSingle);
        registry.Register<double, sbyte>(SysConvert.ToSByte);
        registry.Register<double, string>(from => from.ToString(InvariantCulture));
    }

    private static void RegisterDecimalConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<decimal, short>(SysConvert.ToInt16);
        registry.Register<decimal, int>(SysConvert.ToInt32);
        registry.Register<decimal, long>(SysConvert.ToInt64);
        registry.Register<decimal, ushort>(SysConvert.ToUInt16);
        registry.Register<decimal, uint>(SysConvert.ToUInt32);
        registry.Register<decimal, ulong>(SysConvert.ToUInt64);
        registry.Register<decimal, float>(SysConvert.ToSingle);
        registry.Register<decimal, double>(SysConvert.ToDouble);
        registry.Register<decimal, sbyte>(SysConvert.ToSByte);
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

        registry.Register<List<string>, ImmutableArray<string>>(
            from => from.ToImmutableArray());
    }

    private static void RegisterJsonElementConversions(
        DefaultTypeConverter registry)
    {
        registry.Register<IReadOnlyDictionary<string, object?>, JsonElement>(
            DictionaryToJsonDocumentConverter.FromDictionary);
        registry.Register<JsonElement, IReadOnlyDictionary<string, object?>>(
            DictionaryToJsonDocumentConverter.ToDictionary);
        registry.Register<IReadOnlyList<object?>, JsonElement>(
            DictionaryToJsonDocumentConverter.FromList);
        registry.Register<JsonElement, IReadOnlyList<object?>>(
            DictionaryToJsonDocumentConverter.ToList);
    }
}
