namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Factory for creating FunctionInvocations
    /// </summary>
    public class Functions
    {
        public static FunctionInvocation Exists(Expression expression) =>
            FunctionInvocation.Create(BuiltInFunctions.Predicates.Exists, expression);

        public static FunctionInvocation All(Expression expression) =>
            FunctionInvocation.Create(BuiltInFunctions.Predicates.All, expression);
    }
}
