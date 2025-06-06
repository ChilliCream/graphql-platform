namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLRequestParser
{
    private const byte O = (byte)'o';
    private const byte N = (byte)'n';
    private const byte Q = (byte)'q';
    private const byte V = (byte)'v';
    private const byte E = (byte)'e';
    private const byte T = (byte)'t';
    private const byte I = (byte)'i';
    private const byte P = (byte)'p';

    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    private static ReadOnlySpan<byte> OperationName =>
    [
        (byte)'o',
        (byte)'p',
        (byte)'e',
        (byte)'r',
        (byte)'a',
        (byte)'t',
        (byte)'i',
        (byte)'o',
        (byte)'n',
        (byte)'N',
        (byte)'a',
        (byte)'m',
        (byte)'e'
    ];

    private static ReadOnlySpan<byte> Query =>
    [
        (byte)'q',
        (byte)'u',
        (byte)'e',
        (byte)'r',
        (byte)'y'
    ];

    private static ReadOnlySpan<byte> Variables =>
    [
            (byte)'v',
            (byte)'a',
            (byte)'r',
            (byte)'i',
            (byte)'a',
            (byte)'b',
            (byte)'l',
            (byte)'e',
            (byte)'s'
    ];

    private static ReadOnlySpan<byte> Extensions =>
    [
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
    ];

    private static ReadOnlySpan<byte> Type =>
    [
        (byte)'t',
        (byte)'y',
        (byte)'p',
        (byte)'e'
    ];

    private static ReadOnlySpan<byte> Id =>
    [
        (byte)'i',
        (byte)'d'
    ];

    private static ReadOnlySpan<byte> Payload =>
    [
        (byte)'p',
        (byte)'a',
        (byte)'y',
        (byte)'l',
        (byte)'o',
        (byte)'a',
        (byte)'d'
    ];
}
