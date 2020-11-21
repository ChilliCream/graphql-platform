namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// This wraps a function into a condition so that it can be used in a where clause. The function is supposed to return a
    /// boolean value.
    /// </summary>
    public class BooleanFunctionCondition : Condition
    {
        private readonly FunctionInvocation _fi;

        public BooleanFunctionCondition(FunctionInvocation fi)
        {
            _fi = fi;
        }

        public new void Visit(CypherVisitor visitor)
        {
            _fi.Visit(visitor);
        }
    }
}