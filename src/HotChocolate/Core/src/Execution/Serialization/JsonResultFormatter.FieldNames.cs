using System;

namespace HotChocolate.Execution.Serialization;

public sealed partial class JsonResultFormatter
{
    private static ReadOnlySpan<byte> Data => "data"u8;

    private static ReadOnlySpan<byte> Items => "items"u8;

    private static ReadOnlySpan<byte> Errors => "errors"u8;

    private static ReadOnlySpan<byte> Extensions => "extensions"u8;

    private static ReadOnlySpan<byte> Message => "message"u8;

    private static ReadOnlySpan<byte> Locations => "locations"u8;

    private static ReadOnlySpan<byte> Path => "path"u8;

    private static ReadOnlySpan<byte> Line => "line"u8;

    private static ReadOnlySpan<byte> Column => "column"u8;

    private static ReadOnlySpan<byte> Incremental => "incremental"u8;
    
    private static ReadOnlySpan<byte> RequestIndex => "requestIndex"u8;
    
    private static ReadOnlySpan<byte> VariableIndex => "variableIndex"u8;
}
