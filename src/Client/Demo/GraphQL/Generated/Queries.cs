using System;
using StrawberryShake;

namespace Foo
{
    public class Queries
        : IDocument
    {
        private static readonly byte[] _hashName = new byte[] { };
        private static readonly byte[] _hash = new byte[] { };
        private static readonly byte[] _content = new byte[] { };
        private const string _query = "";

        public ReadOnlySpan<byte> HashName => _hashName;

        public ReadOnlySpan<byte> Hash => _hash;

        public ReadOnlySpan<byte> Content => _content;

        public override string ToString() => _query;

        public static Queries Default { get; } = new Queries();
    }
}
