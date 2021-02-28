using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    public partial class CypherVisitor
    {
        private void EnterVisitable(Match match)
        {
            if (match.IsOptional())
            {
                _writer.Write("OPTIONAL ");
            }
            _writer.Write("MATCH ");
        }

        private void LeaveVisitable(Match match)
        {
            _writer.Write(" ");
        }

        public void EnterVisitable(Where where)
        {
            _writer.Write("WHERE ");
        }

        public void EnterVisitable(Exists exists)
        {
            _writer.Write(" EXISTS ");
        }

        public void EnterVisitable(Create create)
        {
            _writer.Write("CREATE ");
        }

        public void LeaveVisitable(Create create)
        {
            _writer.Write(" ");
        }

        public void EnterVisitable(Node node)
        {
            _writer.Write("(");
        }

        public void LeaveVisitable(Node node)
        {
            _writer.Write(")");
        }

        public void EnterVisitable(SymbolicName symbolicName)
        {
            _writer.Write(symbolicName.GetValue());
        }

        public void EnterVisitable(NodeLabel nodeLabel)
        {
            _writer.Write(Symbol.Colon);
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

        private void EnterVisitable(Properties properties)
        {
            _writer.Write(" ");
        }

        public void EnterVisitable(MapExpression map)
        {
            _writer.Write("{");
        }

        private void EnterVisitable(KeyValueMapEntry map)
        {
            _writer.Write(map.GetKey());
            _writer.Write(": ");
        }

        private void EnterVisitable(KeyValueSeparator map)
        {
            _writer.Write(", ");
        }

        private void LeaveVisitable(MapExpression map)
        {
            _writer.Write("}");
        }

        private void EnterVisitable(PatternComprehension patternComprehension)
        {
            _writer.Write("[");
        }

        private void LeaveVisitable(PatternComprehension patternComprehension)
        {
            _writer.Write("]");
        }


        public void EnterVisitable(ILiteral literal)
        {
            _writer.Write(literal.AsString());
        }

        private void EnterVisitable(CompoundCondition compoundCondition)
        {
            _writer.Write("(");
        }

        private void LeaveVisitable(CompoundCondition compoundCondition)
        {
            _writer.Write(")");
        }

        private void EnterVisitable(NestedExpression nestedExpression)
        {
            _writer.Write("(");
        }

        private void LeaveVisitable(NestedExpression nestedExpression)
        {
            _writer.Write(")");
        }
        private void EnterVisitable(Return @return)
        {
            _writer.Write("RETURN ");
        }

        private void EnterVisitable(Distinct distinct)
        {
            _writer.Write("DISTINCT ");
        }
        private void EnterVisitable(OrderBy orderBy)
        {
            _writer.Write(" ORDER BY ");
        }

        private void EnterVisitable(Skip skip)
        {
            _writer.Write(" SKIP ");
        }

        private void EnterVisitable(Limit limit)
        {
            _writer.Write(" LIMIT ");
        }

        private void EnterVisitable(RelationshipLength length)
        {
            var minimum = length.GetMinimum();
            var maximum = length.GetMaximum();

            if (length.IsUnbounded()) {
                _writer.Write("*");
                return;
            }

            if (minimum == null && maximum == null) {
                return;
            }

            _writer.Write("*");
            if (minimum != null) {
                _writer.Write(minimum.ToString());
            }
            _writer.Write("..");
            if (maximum != null) {
                _writer.Write(maximum.ToString());
            }
        }

        private void EnterVisitable(RelationshipTypes types)
        {
            _writer.Write(string.Join(Symbol.Pipe, types.GetValues));
        }
    }
}
