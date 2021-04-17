namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// List comprehension is a syntactic construct available in Cypher for creating a list based on existing lists.
    /// https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/Atom.html#ListComprehension
    /// </summary>
    public class ListComprehension : Expression
    {
        public override ClauseKind Kind => ClauseKind.ListComprehension;

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

        public ListComprehension(
            SymbolicName variable,
            Expression listExpression,
            Where where,
            Expression listDefinition)
        {
            _variable = variable;
            _listExpression = listExpression;
            _where = where;
            _listDefinition = listDefinition;
        }

        public static IOngoingDefinitionWithVariable With(SymbolicName variable)
        {
            return new Builder(variable);
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

        public interface IOngoingDefinitionWithVariable
        {
            IOngoingDefinitionWithList In(Expression list);
        }

        public interface IOngoingDefinitionWithList : IOngoingDefinitionWithoutReturn
        {
            IOngoingDefinitionWithoutReturn Where(Condition condition);
        }

        public interface IOngoingDefinitionWithoutReturn
        {
            ListComprehension Returning(params INamed[] variables);
            ListComprehension Returning(params Expression[] listDefinition);
            ListComprehension Returning();
        }

        private class Builder : IOngoingDefinitionWithVariable, IOngoingDefinitionWithList
        {
            private readonly SymbolicName _variable;
            private Expression _listExpression;
            private Where _where;

            public Builder(SymbolicName variable)
            {
                _variable = variable;
            }

            public IOngoingDefinitionWithList In(Expression list)
            {
                _listExpression = list;
                return this;
            }

            public IOngoingDefinitionWithoutReturn Where(Condition condition)
            {
                _where = new Where(condition);
                return this;
            }
            public ListComprehension Returning() => new (_variable, _listExpression, _where, null);

            public ListComprehension Returning(params INamed[] variables) =>
                Returning(Expressions.CreateSymbolicNames(variables));

            public ListComprehension Returning(params Expression[] listDefinition) =>
                new (_variable, _listExpression, _where,
                    ListExpression.ListOrSingleExpression(listDefinition));
        }
    }
}
