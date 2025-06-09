namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private const byte D = (byte)'d';
    private const byte O = (byte)'o';
    private const byte Q = (byte)'q';
    private const byte V = (byte)'v';
    private const byte E = (byte)'e';
    private const byte T = (byte)'t';
    private const byte I = (byte)'i';
    private const byte P = (byte)'p';

    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    private static ReadOnlySpan<byte> OperationName => "operationName"u8;

    private static ReadOnlySpan<byte> Query => "query"u8;

    private static ReadOnlySpan<byte> Variables => "variables"u8;

    private static ReadOnlySpan<byte> Extensions => "extensions"u8;

    private static ReadOnlySpan<byte> Type => "type"u8;

    private static ReadOnlySpan<byte> Id => "id"u8;

    private static ReadOnlySpan<byte> DocumentId => "documentId"u8;

    private static ReadOnlySpan<byte> Payload => "payload"u8;
}
