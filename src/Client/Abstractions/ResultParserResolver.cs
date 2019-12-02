using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public sealed class ResultParserResolver
        : IResultParserResolver
    {
        private readonly IReadOnlyDictionary<Type, IResultParser> _parsers;

        public ResultParserResolver(IEnumerable<IResultParser> resultParsers)
        {
            if (resultParsers is null)
            {
                throw new ArgumentNullException(nameof(resultParsers));
            }

            _parsers = resultParsers.ToDictionary();
        }

        public IResultParser GetResultParser(Type resultType)
        {
            if (!_parsers.TryGetValue(resultType, out IResultParser? parser))
            {
                throw new ArgumentException(
                    $"The type `{resultType.FullName}` has no result parser " +
                    "and cannot be handled.");
            }
            return parser;
        }
    }
}
