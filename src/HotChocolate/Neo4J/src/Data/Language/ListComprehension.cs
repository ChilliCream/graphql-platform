namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// List comprehension is a syntactic construct available in Cypher for creating a list based on
    /// existing lists.
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/Atom.html#ListComprehension">
    /// List Comprehension
    /// </a>
    /// </summary>
    public class ListComprehension : Expression
    {
        public ListComprehension(
            SymbolicName variable,
            Expression listExpression,
            Where where,
            Expression listDefinition)
        {
            Variable = variable;
            ListExpression = listExpression;
            Where = where;
            ListDefinition = listDefinition;
        }

        public override ClauseKind Kind => ClauseKind.ListComprehension;

        /// <summary>
        /// The variable for the where part
        /// </summary>
        public SymbolicName Variable {get;}

        /// <summary>
        /// The list expression. No further assertions are taken to check beforehand if it is a
        /// Cypher List.
        /// </summary>
        public Expression ListExpression {get;}

        /// <summary>
        /// Filtering on the list.
        /// </summary>
        public Where Where {get;}

        /// <summary>
        /// The new list to be returned.
        /// </summary>
        public Expression ListDefinition {get;}


        public static IOngoingDefinitionWithVariable With(SymbolicName variable)
        {
            return new Builder(variable);
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Variable.Visit(cypherVisitor);
            Operator.In.Visit(cypherVisitor);
            ListExpression.Visit(cypherVisitor);
            Where?.Visit(cypherVisitor);
            if (ListDefinition != null)
            {
                Operator.Pipe.Visit(cypherVisitor);
                ListDefinition.Visit(cypherVisitor);
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

            public ListComprehension Returning() => new(_variable, _listExpression, _where, null);

            public ListComprehension Returning(params INamed[] variables) =>
                Returning(Expressions.CreateSymbolicNames(variables));

            public ListComprehension Returning(params Expression[] listDefinition) =>
                new(_variable,
                    _listExpression,
                    _where,
                    Language.ListExpression.ListOrSingleExpression(listDefinition));
        }
    }
}
