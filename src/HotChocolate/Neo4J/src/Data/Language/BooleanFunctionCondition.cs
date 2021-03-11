namespace HotChocolate.Data.Neo4J.Language
{
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
