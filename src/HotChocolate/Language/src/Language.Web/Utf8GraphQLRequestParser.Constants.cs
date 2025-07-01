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
    private static ReadOnlySpan<byte> OperationNameProperty => "operationName"u8;

    private static ReadOnlySpan<byte> QueryProperty => "query"u8;

    private static ReadOnlySpan<byte> VariablesProperty => "variables"u8;

    private static ReadOnlySpan<byte> ExtensionsProperty => "extensions"u8;

    private static ReadOnlySpan<byte> TypeProperty => "type"u8;

    private static ReadOnlySpan<byte> IdProperty => "id"u8;

    private static ReadOnlySpan<byte> DocumentIdProperty => "documentId"u8;

    private static ReadOnlySpan<byte> PayloadProperty => "payload"u8;
}
