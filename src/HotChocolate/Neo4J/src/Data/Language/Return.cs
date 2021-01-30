using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// The RETURN clause defines what to include in the query result set.
    /// </summary>
    public class Return : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Return;
        private readonly Distinct? _distinct;
        private readonly List<Expression> _expressions;
        private readonly OrderBy? _order;
        private readonly Skip? _skip;
        private readonly Limit? _limit;

        public Return(bool distinct, List<Expression> expressions, OrderBy? orderBy, Skip? skip, Limit? limit) {
            _distinct = distinct ? Distinct.Instance : null;
            _expressions = expressions;
            _order = orderBy;
            _skip = skip;
            _limit = limit;
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _distinct?.Visit(visitor);
            _expressions.ForEach(element => element.Visit(visitor));
            _order?.Visit(visitor);
            _skip?.Visit(visitor);
            _limit?.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
