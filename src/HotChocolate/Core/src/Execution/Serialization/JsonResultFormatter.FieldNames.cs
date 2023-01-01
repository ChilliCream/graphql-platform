using System;

namespace HotChocolate.Execution.Serialization;

public sealed partial class JsonResultFormatter
{
    private static ReadOnlySpan<byte> Data
        => new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };

    private static ReadOnlySpan<byte> Items
        => new[] { (byte)'i', (byte)'t', (byte)'e', (byte)'m', (byte)'s' };

    private static ReadOnlySpan<byte> Errors
        => new[] { (byte)'e', (byte)'r', (byte)'r', (byte)'o', (byte)'r', (byte)'s' };

    private static ReadOnlySpan<byte> Extensions
        => new[]
        {
            (byte)'e', (byte)'x', (byte)'t', (byte)'e', (byte)'n', (byte)'s', (byte)'i',
            (byte)'o', (byte)'n', (byte)'s'
        };

    private static ReadOnlySpan<byte> Message
        => new[]
        {
            (byte)'m', (byte)'e', (byte)'s', (byte)'s', (byte)'a', (byte)'g', (byte)'e'
        };

    private static ReadOnlySpan<byte> Locations
        => new[]
        {
            (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'t', (byte)'i', (byte)'o',
            (byte)'n', (byte)'s'
        };

    private static ReadOnlySpan<byte> Path
        => new[] { (byte)'p', (byte)'a', (byte)'t', (byte)'h' };

    private static ReadOnlySpan<byte> Line
        => new[] { (byte)'l', (byte)'i', (byte)'n', (byte)'e' };

    private static ReadOnlySpan<byte> Column
        => new[] { (byte)'c', (byte)'o', (byte)'l', (byte)'u', (byte)'m', (byte)'n' };

    private static ReadOnlySpan<byte> Incremental
        => new[]
        {
            (byte)'i', (byte)'n', (byte)'c', (byte)'r', (byte)'e', (byte)'m', (byte)'e',
            (byte)'n', (byte)'t', (byte)'a', (byte)'l'
        };
}
