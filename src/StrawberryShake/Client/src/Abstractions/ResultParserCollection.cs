using System;
using System.Collections;
using System.Collections.Generic;

namespace StrawberryShake
{
    public sealed class ResultParserCollection
        : IResultParserCollection
    {
        private readonly IReadOnlyDictionary<Type, IResultParser> _parsers;

        public ResultParserCollection(IReadOnlyDictionary<Type, IResultParser> parsers)
        {
            _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
            Count = _parsers.Count;
        }

        public int Count { get; }

        public IResultParser Get(Type resultType)
        {
            if (!_parsers.TryGetValue(resultType, out IResultParser? parser))
            {
                throw new ArgumentException(
                    $"The type `{resultType.FullName}` has no result parser " +
                    "and cannot be handled.");
            }
            return parser;
        }

        public IEnumerator<IResultParser> GetEnumerator()
        {
            return _parsers.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
