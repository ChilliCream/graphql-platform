using System.Collections.Generic;
using System.Linq;

#nullable enable

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

        public Return(bool distinct, Expression[] expressions, OrderBy? orderBy = null, Skip? skip = null, Limit? limit = null) {
            _distinct = distinct ? Distinct.Instance : null;
            _expressions = expressions.ToList();
            _order = orderBy;
            _skip = skip;
            _limit = limit;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _distinct?.Visit(cypherVisitor);
            _expressions.ForEach(element => element.Visit(cypherVisitor));
            _order?.Visit(cypherVisitor);
            _skip?.Visit(cypherVisitor);
            _limit?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
