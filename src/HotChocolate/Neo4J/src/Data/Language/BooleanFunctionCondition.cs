namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// This wraps a function into a condition so that it can be used in a where clause.
    /// The function is supposed to return a boolean value.
    /// </summary>
    public class BooleanFunctionCondition : Condition
    {
        public BooleanFunctionCondition(FunctionInvocation functionInvocation)
        {
            FunctionInvocation = functionInvocation;
        }

        public override ClauseKind Kind => ClauseKind.BooleanFunctionCondition;

        public FunctionInvocation FunctionInvocation { get; }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            FunctionInvocation.Visit(cypherVisitor);
        }
    }
}
