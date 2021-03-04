using System.Linq;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    public class StatementBuilder
    {
        private bool _isWrite;
        private Create? _create;
        private Match? _match;
        private Where? _where;
        private Return? _return;

        //private readonly string Order;
        //private readonly string Limit;

        public StatementBuilder Create(params IPatternElement[] elements)
        {
            _isWrite = true;
            _create = new Create(new Pattern(elements.ToList()));
            return this;
        }

        public StatementBuilder OptionalMatch(params IPatternElement[] elements) =>
            Match(true, elements);

        public StatementBuilder Match(params IPatternElement[] elements) =>
            Match(false, elements);

        private StatementBuilder Match(bool optional, params IPatternElement[] elements)
        {
            _match = new Match(optional, new Pattern(elements.ToList()), null);
            return this;
        }

        public StatementBuilder Return(params INamed[] elements)
        {
            _return = new Return(false, new ExpressionList(Expressions.CreateSymbolicNames(elements)));
            return this;
        }

        public StatementBuilder Return(params Expression[] expressions)
        {
            _return = new Return(false, new ExpressionList(expressions));
            return this;
        }

        public StatementBuilder Where(Condition condition)
        {
            _where = new Where(condition);
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
                    _create?.Visit(visitor);
                    _return?.Visit(visitor);
                    break;
            }
            return visitor.Print();
        }
    }
}
