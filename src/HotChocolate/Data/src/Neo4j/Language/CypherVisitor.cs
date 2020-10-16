using System;

namespace HotChocolate.Data.Neo4j
{
    public class CypherVisitor
    {
        private readonly CypherWriter _writer = new CypherWriter();

        public void VisitIfNotNull(IVisitable? visitable)
        {
            if (visitable is not null)
            {
                visitable.Visit(this);
            }
        }

        public void Enter(Node node)
        {
            _writer.Write($"({node.Alias ?? ""}");
        }

        public void Leave(Node node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            _writer.Write(")");
        }

        public void Enter(Match match)
        {
            if (match.IsOptional)
            {
                _writer.Write("OPTIONAL ");
            }
            _writer.Write("MATCH ");
        }

        public void Leave(Match match)
        {
            _writer.Write(" ");
        }

        public void Enter(Return @return)
        {
            _writer.Write($"RETURN ({@return.Node.Alias})");
        }

        public void Leave(Return @return)
        {
            _writer.Write(" ");
        }

        public void Enter(NodeLabels labels)
        {
            foreach(var label in labels.GetLabels()){
                _writer.Write($"{Symbols.NodeLabelStart}{label}");
            }
        }

        public void Leave(NodeLabels labels)
        {
            _writer.Write("");
        }

        public void Enter(Property property)
        {
            _writer.Write($"{property.Key} {property.Operator} {property.Parameter}");
        }

        public void Leave(Property property)
        {

        }

        public void Enter(Raw raw)
        {
            _writer.Write(raw.Value);
        }

        public void Leave(Raw raw)
        {

        }


        public override string ToString()
        {
            return _writer.Print();
        }
    }
}
