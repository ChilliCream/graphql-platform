using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotChocolate.Data.Neo4j
{
#pragma warning disable CA1716 // Identifiers should not match keywords
    public class Return : IVisitable
#pragma warning restore CA1716 // Identifiers should not match keywords
    {
        public Node Node { get; private set; }

        public Return(Node node)
        {
            Node = node;
        }

        public void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.Leave(this);
        }
    }
}
