using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotChocolate.Data.Neo4j
{
    public class Node : IVisitable
    {
        private readonly string _alias;
        private readonly NodeLabels _labels;
        //private readonly Properties? _poperties;

        public Node(string alias, NodeLabels labels)
        {
            _alias = alias;
            _labels = labels;
        }

        public Node(string alias, List<string> labels)
        {

            _alias = alias;
            _labels = new NodeLabels(labels);
        }

        public string Alias => _alias;
        public string PrimaryLabel => _labels.GetLabels()[0];

        public void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.VisitIfNotNull(_labels);
            visitor.Leave(this);
        }

        public Node Named(string alias)
        {
            return new Node(alias, _labels);
        }
    }
}
