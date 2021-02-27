using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Delete = [(D,E,T,A,C,H), SP], (D,E,L,E,T,E), [SP], Expression, { [SP], ',', [SP], Expression } ;
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Delete.html">Delete</a>.
    /// </summary>
    public class Delete : Visitable, IUpdatingClause
    {
        public override ClauseKind Kind => ClauseKind.Delete;
        private readonly List<Expression> _deleteItems;
        private readonly bool _detach;

        public Delete(List<Expression> deleteItems, bool detatch)
        {
            _deleteItems = deleteItems;
            _detach = detatch;
        }

        public bool IsDetach => _detach;

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _deleteItems.ForEach(i => PrepareVisit(i).Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }

        private static Visitable PrepareVisit(Expression child) {
            return Expressions.NameOrExpression(child);
        }
    }
}
