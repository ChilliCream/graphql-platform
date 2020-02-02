using System;

namespace StrawberryShake.Http
{
    public abstract partial class JsonResultParserBase<T>
        : IResultParser
        where T : class
    {
        private static readonly byte[] _data = new byte[]
        {
            (byte)'d',
            (byte)'a',
            (byte)'t',
            (byte)'a'
        };

        private static readonly byte[] _errors = new byte[]
        {
            (byte)'e',
            (byte)'r',
            (byte)'r',
            (byte)'o',
            (byte)'r',
            (byte)'s'
        };

        private static readonly byte[] _extensions = new byte[]
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
            (byte)'s',
        };

        private static readonly byte[] _typename = new byte[]
        {
            (byte)'_',
            (byte)'_',
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e',
            (byte)'n',
            (byte)'a',
            (byte)'m',
            (byte)'e',
        };

        private static readonly byte[] _message = new byte[]
        {
            (byte)'m',
            (byte)'e',
            (byte)'s',
            (byte)'s',
            (byte)'a',
            (byte)'g',
            (byte)'e'
        };

        private static readonly byte[] _locations = new byte[]
        {
            (byte)'l',
            (byte)'o',
            (byte)'c',
            (byte)'a',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n',
            (byte)'s'
        };

        private static readonly byte[] _path = new byte[]
        {
            (byte)'p',
            (byte)'a',
            (byte)'t',
            (byte)'h'
        };

        private static readonly byte[] _line = new byte[]
        {
            (byte)'l',
            (byte)'i',
            (byte)'n',
            (byte)'e'
        };

        private static readonly byte[] _column = new byte[]
        {
            (byte)'c',
            (byte)'o',
            (byte)'l',
            (byte)'u',
            (byte)'m',
            (byte)'n'
        };
    }
}
