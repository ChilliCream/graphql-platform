using System.Collections;
using System.Collections.Generic;

namespace Generator.ClassGenerator
{
    public class Block : IEnumerable<Statement>
    {
        private readonly List<Statement> _statements = new List<Statement>();

        public Block(params Statement[] statements)
        {
            _statements.AddRange(statements);
        }

        public void Add(Statement statement)
        {
            _statements.Add(statement);
        }

        public void Add(Block block)
        {
            _statements.AddRange(block);
        }

        public IEnumerator<Statement> GetEnumerator()
        {
            return _statements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_statements).GetEnumerator();
        }
    }
}