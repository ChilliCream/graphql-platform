using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    internal class QueryParsing : IAction
    {
        private readonly bool _parse;

        public QueryParsing(bool parse)
        {
            _parse = parse;
        }

        public Block CreateBlock(Statement header)
        {
            return new Block
            {
                header,
                new Statement("DocumentNode document = _parser.Parse(query);")
            };
        }
    }
}
