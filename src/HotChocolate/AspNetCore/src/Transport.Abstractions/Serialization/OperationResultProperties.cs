using System;

namespace HotChocolate.Transport.Sockets.Client.Helpers;

internal static class OperationResultProperties
{
    public static ReadOnlySpan<byte> DataProp => "data"u8;

    public static ReadOnlySpan<byte> ErrorsProp => "errors"u8;

    public static ReadOnlySpan<byte> ExtensionsProp => "extensions"u8;
}
