using System.Buffers.Text;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Composes and reads the <c>_{index}_</c> prefix that identifies the item a root selection, a
/// variable, or an error path of a batched request belongs to.
/// </summary>
internal static class AliasBatchPrefixHelper
{
    // An underscore, the decimal digits of a non-negative int and a second underscore.
    private const int MaxPrefixLength = 12;

    /// <summary>
    /// Writes the prefix of the item at <paramref name="index"/> into <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">
    /// The buffer that receives the prefix. It must hold at least
    /// <see cref="MaxPrefixLength"/> bytes.
    /// </param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The written prefix.</returns>
    public static ReadOnlySpan<byte> ComposePrefix(Span<byte> buffer, int index)
    {
        buffer[0] = (byte)'_';
        Utf8Formatter.TryFormat(index, buffer[1..], out var written);
        buffer[written + 1] = (byte)'_';
        return buffer[..(written + 2)];
    }

    /// <summary>
    /// Writes the prefixed name of <paramref name="name"/> for the item at
    /// <paramref name="index"/> into <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">
    /// The buffer that receives the name. It must hold at least <see cref="MaxPrefixLength"/>
    /// bytes more than <paramref name="name"/>.
    /// </param>
    /// <param name="index">The zero-based item index.</param>
    /// <param name="name">The name that is prefixed.</param>
    /// <returns>The written name.</returns>
    public static ReadOnlySpan<byte> ComposeName(
        Span<byte> buffer,
        int index,
        ReadOnlySpan<byte> name)
    {
        var prefix = ComposePrefix(buffer, index);
        name.CopyTo(buffer[prefix.Length..]);
        return buffer[..(prefix.Length + name.Length)];
    }

    /// <summary>
    /// Returns the buffer length required to compose a prefixed form of a name of
    /// <paramref name="nameLength"/> bytes.
    /// </summary>
    /// <param name="nameLength">The length of the name that is prefixed.</param>
    public static int GetNameBufferLength(int nameLength) => nameLength + MaxPrefixLength;

    /// <summary>
    /// Reads the item index from a prefixed name.
    /// </summary>
    /// <param name="prefixedName">The name to read.</param>
    /// <param name="index">The zero-based item index the name belongs to.</param>
    /// <param name="name">The name without its prefix.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="prefixedName"/> carries a prefix; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool TryReadIndex(
        ReadOnlySpan<byte> prefixedName,
        out int index,
        out ReadOnlySpan<byte> name)
    {
        index = 0;
        name = default;

        if (prefixedName.Length < 3 || prefixedName[0] != (byte)'_')
        {
            return false;
        }

        var value = 0;
        var position = 1;

        while (position < prefixedName.Length)
        {
            var digit = prefixedName[position] - (byte)'0';

            if ((uint)digit > 9)
            {
                break;
            }

            // An index that does not fit into an int names no item of the batch.
            if (value > (int.MaxValue - digit) / 10)
            {
                return false;
            }

            value = (value * 10) + digit;
            position++;
        }

        if (position == 1 || position == prefixedName.Length || prefixedName[position] != (byte)'_')
        {
            return false;
        }

        index = value;
        name = prefixedName[(position + 1)..];
        return true;
    }
}
