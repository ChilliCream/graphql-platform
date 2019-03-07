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

        public Block CreateBlock()
        {
            return new Block
            {
                new Statement("DocumentNode document = _parser.Parse(query);")
            };
        }
    }
}
