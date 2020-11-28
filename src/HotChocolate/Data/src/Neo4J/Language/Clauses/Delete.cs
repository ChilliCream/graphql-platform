namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Delete = [(D,E,T,A,C,H), SP], (D,E,L,E,T,E), [SP], Expression, { [SP], ',', [SP], Expression } ;
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Delete.html">Delete</a>.
    /// </summary>
    public class Delete : Visitable, IUpdatingClause
    {
        public new ClauseKind Kind { get; } = ClauseKind.Delete;
        private readonly ExpressionList _deleteItems;
        private readonly bool _detach;

        public Delete(ExpressionList deleteItems, bool detatch)
        {
            _deleteItems = deleteItems;
            _detach = detatch;
        }

        public bool IsDetach => _detach;

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _deleteItems.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
