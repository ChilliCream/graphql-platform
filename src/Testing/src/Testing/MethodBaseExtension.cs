using System.Buffers;
using System.Globalization;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Testing;

/// <summary>
/// The method base extension is used to add more functionality
/// to the class <see cref="MethodBase"/>
/// </summary>
internal static class MethodBaseExtension
{
    /// <summary>
    /// Creates the name of the method with class name.
    /// </summary>
    /// <param name="methodBase">The used method name to get the name.</param>
    public static string ToName(this MethodBase methodBase)
        => string.Concat(
            methodBase.ReflectedType!.Name.ToString(CultureInfo.InvariantCulture), ".",
            methodBase.Name.ToString(CultureInfo.InvariantCulture), ".snap");
}

internal static class WriterHelper
{
    private static readonly Encoding _utf8 = Encoding.UTF8;

#if NET5_0_OR_GREATER
    public static void Append(this IBufferWriter<byte> snapshot, string value)
#else
    public unsafe static void Append(this IBufferWriter<byte> snapshot, string value)
#endif
    {
#if NET5_0_OR_GREATER
        _utf8.GetBytes(value.AsSpan(), snapshot);
#else
        var length = _utf8.GetByteCount(value);
        byte[]? buffer = null;
        var span = length <= 256
            ? stackalloc byte[length]
            : buffer = ArrayPool<byte>.Shared.Rent(length);
        var written = 0;

        fixed (byte* bptr = span)
        {
            fixed (char* cprt = value)
            {
                written = _utf8.GetBytes(cprt, value.Length, bptr, length);
            }
        }

        var snapshotSpan = snapshot.GetSpan(written);
        span[..written].CopyTo(snapshotSpan);
        snapshotSpan = snapshotSpan[written..];
        snapshot.Advance(written);

        if (buffer is not null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
#endif
    }

    public static void AppendLine(this IBufferWriter<byte> snapshot)
    {
        snapshot.GetSpan(1)[0] = (byte)'\n';
        snapshot.Advance(1);
    }

    public static void AppendSeparator(this IBufferWriter<byte> snapshot)
    {
        const byte hyphen = (byte)'-';
        var span = snapshot.GetSpan(15);

        for(var i = 0; i < 15; i++)
        {
            span[i] = hyphen;
        }

        snapshot.Advance(15);
    }
}
