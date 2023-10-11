using System;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private const byte _o = (byte)'o';
    private const byte _n = (byte)'n';
    private const byte _q = (byte)'q';
    private const byte _v = (byte)'v';
    private const byte _e = (byte)'e';
    private const byte _t = (byte)'t';
    private const byte _i = (byte)'i';
    private const byte _p = (byte)'p';

    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    private static ReadOnlySpan<byte> OperationName => "operationName"u8;

    private static ReadOnlySpan<byte> Query => "query"u8;

    private static ReadOnlySpan<byte> Variables => "variables"u8;

    private static ReadOnlySpan<byte> Extensions => "extensions"u8;

    private static ReadOnlySpan<byte> Type => "type"u8;

    private static ReadOnlySpan<byte> Id => "id"u8;

    private static ReadOnlySpan<byte> Payload => "payload"u8;
}
