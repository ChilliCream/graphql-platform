namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// This wraps a function into a condition so that it can be used in a where clause.
    /// The function is supposed to return a boolean value.
    /// </summary>
    public class BooleanFunctionCondition : Condition
    {
        public override ClauseKind Kind { get; } = ClauseKind.BooleanFunctionCondition;

        private readonly FunctionInvocation _functionInvocation;

        public BooleanFunctionCondition(FunctionInvocation functionInvocation)
        {
            _functionInvocation = functionInvocation;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            _functionInvocation.Visit(cypherVisitor);
        }
    }
}
