using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Return.html
    /// Return = (R,E,T,U,R,N), ProjectionBody ;
    /// </summary>
    public class Return : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Return;
        private readonly Distinct? _distinct;
        private readonly ProjectionBody _projectionBody;

        public Return(bool distinct, ExpressionList returnItems, OrderBy? orderBy, Skip? skip, Limit? limit) {
            _distinct = distinct ? Distinct.Instance : null;
            _projectionBody = new ProjectionBody(returnItems, orderBy, skip, limit);
        }

        public Return(bool distinct, params INamed[] nodes)
        {
            _distinct = distinct ? Distinct.Instance : null;

            Expression[] expressions = Expressions.CreateSymbolicNames(nodes);
            _projectionBody = new ProjectionBody(new
                ExpressionList(Expressions.CreateSymbolicNames(nodes)),
                null,
                null,
                null
            );
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _distinct?.Visit(visitor);
            _projectionBody.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
