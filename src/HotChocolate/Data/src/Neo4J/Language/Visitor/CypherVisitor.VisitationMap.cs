using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        public void EnterVisitable(Match match)
        {
            if (match.IsOptional())
            {
                _writer.Write("OPTIONAL ");
            }
            _writer.Write("MATCH ");
        }

        public void LeaveVistable(Match match)
        {
            _writer.Write(" ");
        }

        public void EnterVisitable(Where where)
        {
            _writer.Write(" WHERE ");
        }

        public void EnterVisitable(Create create)
        {
            _writer.Write("CREATE ");
        }

        public void LeaveVistable(Create create)
        {
            _writer.Write(" ");
        }

        public void EnterVisitable(Node node)
        {
            _writer.Write("(");
        }

        public void LeaveVistable(Node node)
        {
            _writer.Write(")");
        }

        public void EnterVisitable(SymbolicName symbolicName)
        {
            _writer.Write(symbolicName.GetValue());
        }

        public void EnterVisitable(NodeLabel nodeLabel)
        {
            _writer.Write(Symbol.NodeLabelStart);
            _writer.Write(nodeLabel.GetValue());
        }

        public void EnterVisitable(PropertyLookup propertyLookup)
        {
            _writer.Write(".");
            _writer.Write(propertyLookup.GetPropertyKeyName());
        }

        public void EnterVisitable(Operator op)
        {
            Operator.Type type = op.GetType();
            if (type == Operator.Type.Label) {
                return;
            }
            if (type != Operator.Type.Prefix && op != Operator.Exponent) {
                _writer.Write(" ");
            }
            _writer.Write(op.GetRepresentation());
            if (type != Operator.Type.Postfix && op != Operator.Exponent) {
                _writer.Write(" ");
            }
        }

        public void EnterVisitable(MapExpression map)
        {
            _writer.Write(" {");
        }

        public void EnterVisitable(KeyValueMapEntry map)
        {
            _writer.Write(map.GetKey());
            _writer.Write(": ");
        }

        public void LeaveVistable(MapExpression map)
        {
            _writer.Write(" }");
        }

        public void EnterVisitable(ILiteral literal)
        {
            _writer.Write(literal.AsString());
        }
    }
}
