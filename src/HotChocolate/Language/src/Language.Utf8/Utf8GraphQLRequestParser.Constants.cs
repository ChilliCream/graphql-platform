using System;

namespace HotChocolate.Language
{
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
        private static ReadOnlySpan<byte> OperationName => new byte[]
        {
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
        };

        private static ReadOnlySpan<byte> QueryName => new byte[]
        {
            (byte)'n',
            (byte)'a',
            (byte)'m',
            (byte)'e',
            (byte)'d',
            (byte)'Q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        private static ReadOnlySpan<byte> Query => new byte[]
        {
            (byte)'q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        private static ReadOnlySpan<byte> Variables => new byte[]
        {
            (byte)'v',
            (byte)'a',
            (byte)'r',
            (byte)'i',
            (byte)'a',
            (byte)'b',
            (byte)'l',
            (byte)'e',
            (byte)'s'
        };

        private static ReadOnlySpan<byte> Extensions => new byte[]
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

        private static ReadOnlySpan<byte> Type => new byte[]
        {
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e'
        };

        private static ReadOnlySpan<byte> Id => new byte[]
        {
            (byte)'i',
            (byte)'d'
        };

        private static ReadOnlySpan<byte> Payload => new byte[]
        {
            (byte)'p',
            (byte)'a',
            (byte)'y',
            (byte)'l',
            (byte)'o',
            (byte)'a',
            (byte)'d'
        };
    }
}
