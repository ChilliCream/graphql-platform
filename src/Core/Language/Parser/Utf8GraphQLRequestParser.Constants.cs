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
        private const byte _d = (byte)'d';

        private static readonly byte[] _operationName = new[]
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

        private static readonly byte[] _queryName = new[]
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

        private static readonly byte[] _query = new[]
        {
            (byte)'q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        private static readonly byte[] _variables = new[]
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

        private static readonly byte[] _extensions = new[]
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

        private static readonly byte[] _type = new[]
        {
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e'
        };

        private static readonly byte[] _id = new[]
        {
            (byte)'i',
            (byte)'d'
        };

        private static readonly byte[] _docId = new[]
        {
            (byte)'d',
            (byte)'o',
            (byte)'c',
            (byte)'_',
            (byte)'i',
            (byte)'d'
        };

        private static readonly byte[] _payload = new[]
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
