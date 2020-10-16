using System.Collections.Generic;

namespace HotChocolate.Data.Neo4j
{
    public class NodeLabels : IVisitable
    {
        private readonly List<string> _values = new List<string>();

        public NodeLabels() { }

        public NodeLabels(List<string> values)
        {
            _values.AddRange(values);
        }

        public void AddLabel(string label)
        {
            _values.Add(label);
        }

        public void Visit(CypherVisitor visitor)
        {
            
        }

        public List<string> GetLabels() => _values;
    }
}
