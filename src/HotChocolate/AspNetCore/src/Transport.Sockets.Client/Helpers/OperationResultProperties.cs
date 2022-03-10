using System;

namespace HotChocolate.Transport.Sockets.Client.Helpers;

internal static class OperationResultProperties
{
    public static ReadOnlySpan<byte> DataProp
        => new[]
        {
            (byte)'d',
            (byte)'a',
            (byte)'t',
            (byte)'a'
        };

    public static ReadOnlySpan<byte> ErrorsProp
        => new[]
        {
            (byte)'e',
            (byte)'r',
            (byte)'r',
            (byte)'o',
            (byte)'r',
            (byte)'s'
        };

    public static ReadOnlySpan<byte> ExtensionsProp
        => new[]
        {
            (byte)'e',
            (byte)'x',
            (byte)'t',
            (byte)'e',
            (byte)'n',
            (byte)'s',
            (byte)'i',
            (byte)'o',
            (byte)'n',
            (byte)'s'
        };
}
