using System;

namespace HotChocolate.Transport.Abstractions.Helpers;

public static class Utf8MessageProperties
{

    // This uses C# compiler's ability to refer to static data directly.
    // For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    public static ReadOnlySpan<byte> IdProp => "id"u8;

    public static ReadOnlySpan<byte> QueryProp => "query"u8;

    public static ReadOnlySpan<byte> TypeProp => "type"u8;

    public static ReadOnlySpan<byte> PayloadProp => "payload"u8;

    public static ReadOnlySpan<byte> DataProp => "data"u8;

    public static ReadOnlySpan<byte> ErrorsProp => "errors"u8;

    public static ReadOnlySpan<byte> ExtensionsProp => "extensions"u8;

    public static ReadOnlySpan<byte> VariablesProp => "variables"u8;

    public static ReadOnlySpan<byte> OperationNameProp => "operationName"u8;
}
