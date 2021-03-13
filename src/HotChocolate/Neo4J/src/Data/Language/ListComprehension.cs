namespace HotChocolate.Data.Neo4J.Language
{
    public class ListComprehension : Expression
    {
        public override ClauseKind Kind { get; } = ClauseKind.ListComprehension;

        /// <summary>
        /// The variable for the where part
        /// </summary>
        private readonly SymbolicName _variable;

        /// <summary>
        /// The list expression. No further assertions are taken to check beforehand if it is actually a Cypher List atm.
        /// </summary>
        private readonly Expression _listExpression;

        /// <summary>
        /// Filtering on the list.
        /// </summary>
        private readonly Where _where;

        /// <summary>
        /// The new list to be returned.
        /// </summary>
        private readonly Expression _listDefinition;

        private ListComprehension(SymbolicName variable, Expression listExpression,
            Where where, Expression listDefinition) {
            _variable = variable;
            _listExpression = listExpression;
            _where = where;
            _listDefinition = listDefinition;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _variable.Visit(cypherVisitor);
            Operator.In.Visit(cypherVisitor);
            _listExpression.Visit(cypherVisitor);
            _where?.Visit(cypherVisitor);
            if (_listDefinition != null)
            {
                Operator.Pipe.Visit(cypherVisitor);
                _listDefinition.Visit(cypherVisitor);
            }
            cypherVisitor.Leave(this);
        }
    }
}
