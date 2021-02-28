using System.Linq;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    public class StatementBuilder
    {
        private bool _isWrite = false;
        private Match? _match;
        private Where? _where;
        private Return? _return;
        //private readonly string Order;
        //private readonly string Limit;

        public StatementBuilder OptionalMatch(params PatternElement[] elements) =>
            Match(true, elements);
        public StatementBuilder Match(params PatternElement[] elements) =>
            Match(false, elements);

        private StatementBuilder Match(bool optional, params PatternElement[] elements)
        {
            _match = new Match(optional, new Pattern(elements.ToList()), null);
            return this;
        }

        public StatementBuilder Return(params INamed[] elements)
        {
            _return = new Return(false, Expressions.CreateSymbolicNames(elements));
            return this;
        }

        public StatementBuilder Return(ExpressionList elements)
        {
            _return = new Return(false, elements);
            return this;
        }


        public string Build()
        {
            var visitor = new CypherVisitor();
            switch (_isWrite)
            {
                case false:
                    _match?.Visit(visitor);
                    _where?.Visit(visitor);
                    _return?.Visit(visitor);
                    break;
                default:
                    // TODO: Write Implementation
                    break;
            }
            return visitor.Print();
        }

        public IStatementBuilder GetDefaultBuilder()
        {
            throw new System.NotImplementedException();
        }
    }
}
