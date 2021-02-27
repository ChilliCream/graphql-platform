using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/With.html
    /// </summary>
    public class With : Visitable
    {
        public override ClauseKind Kind => ClauseKind.With;

        private readonly Distinct _distinct;
        private readonly Where _where;
        private readonly List<Expression> _expressions;
        private readonly OrderBy _orderBy;
        private readonly Skip _skip;
        private readonly Limit _limit;

        public With(bool distinct, List<Expression> expressions, OrderBy orderBy, Skip skip, Limit limit, Where where)
        {
            _distinct = distinct ? Distinct.Instance : null;
            _expressions = expressions;
            _orderBy = orderBy;
            _skip = skip;
            _limit = limit;
            _where = where;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _distinct?.Visit(cypherVisitor);
            _expressions.ForEach(element => element.Visit(cypherVisitor));
            _orderBy?.Visit(cypherVisitor);
            _skip?.Visit(cypherVisitor);
            _limit?.Visit(cypherVisitor);
            _where?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
