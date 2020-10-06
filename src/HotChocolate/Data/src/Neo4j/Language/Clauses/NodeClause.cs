using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotChocolate.Data.Neo4j.Language.Clauses
{
    public class NodeClause : IPatternElement
    {
        private string _symbolicName;
        private string _label;

        public NodeClause(string symbolicName, string label)
        {
            _symbolicName = symbolicName;
            _label = label;
        }

        public string SymbolicName => _symbolicName;
        public string Label => _label;

        public void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.Leave(this);
        }
    }
}
