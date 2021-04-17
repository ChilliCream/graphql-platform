using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data.Neo4J.Filtering;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    public class StatementBuilder
    {
        private Match? _match;
        private Return? _return;

        private OrderBy? _orderBy;
        private Skip? _skip;
        private Limit? _limit;

        public StatementBuilder Match(params IPatternElement[] elements) =>
            Match(false, elements);

        public StatementBuilder Match(Where? optionalWhere, params IPatternElement[] elements) =>
            Match(false, optionalWhere, elements);

        private StatementBuilder Match(bool optional, params IPatternElement[] elements)
        {
            _match = new Match(optional, new Pattern(elements.ToList()), null);
            return this;
        }

        private StatementBuilder Match(bool optional, Where? optionalWhere, params IPatternElement[] elements)
        {
            _match = new Match(optional, new Pattern(elements.ToList()), optionalWhere);
            return this;
        }

        public StatementBuilder Return(params INamed[] elements)
        {
            _return = new Return(false, new ExpressionList(Expressions.CreateSymbolicNames(elements)));
            return this;
        }

        public StatementBuilder OrderBy(params SortItem[] items)
        {
            _orderBy = new OrderBy(items.ToList());
            return this;
        }

        public StatementBuilder OrderBy(List<SortItem> items)
        {
            _orderBy = new OrderBy(items);
            return this;
        }

        public StatementBuilder Return(params Expression[]? expressions)
        {
            _return = new Return(false, new ExpressionList(expressions));
            return this;
        }

        public StatementBuilder Where(Condition condition)
        {
            //_where = new Where(condition);
            return this;
        }

        public StatementBuilder Skip(int skipNumber)
        {
            _skip = new Skip(skipNumber);
            return this;
        }

        public StatementBuilder Limit(int limitNumber)
        {
            _limit = new Limit(limitNumber);
            return this;
        }

        public string Build()
        {
            var visitor = new CypherVisitor();

            _match?.Visit(visitor);
            _return?.Visit(visitor);
            _orderBy?.Visit(visitor);
            _skip?.Visit(visitor);
            _limit?.Visit(visitor);

            return visitor.Print();
        }
    }
}
