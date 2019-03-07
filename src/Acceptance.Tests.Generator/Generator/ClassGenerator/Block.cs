using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public Statement this[int index] => _statements.ElementAt(index);

        public bool Replace(Statement placeholder, Block whenBlock)
        {
            var placeholderIndex = _statements.IndexOf(placeholder);
            if (placeholderIndex != -1)
            {
                _statements.RemoveAt(placeholderIndex);
                for (int i = 0; i < whenBlock.Count(); i++)
                {
                    _statements.Insert(placeholderIndex + i, whenBlock[i]);
                }

                return true;
            }

            return false;
        }
    }
}
