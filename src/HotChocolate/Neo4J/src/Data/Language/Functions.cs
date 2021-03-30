namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Factory for creating FunctionInvocations
    /// </summary>
    public class Functions
    {
        /// <summary>
        /// https://neo4j.com/docs/cypher-manual/current/functions/scalar/#functions-id
        /// </summary>
        /// <param name="node">The node for which the internal id should be retrieved</param>
        /// <returns>A function call for id on a node.</returns>
        public static FunctionInvocation Id(Node node)
        {
            Ensure.IsNotNull(node, "The node for id() is required.");

            return FunctionInvocation.Create(BuiltInFunctions.Scalars.Id, node.GetRequiredSymbolicName());
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="relationship">The relationship for which the internal id should be retrieved.</param>
        /// <returns></returns>
        public static FunctionInvocation Id(Relationship relationship)
        {

            Ensure.IsNotNull(relationship, "The relationship for id() is required.");

            return FunctionInvocation.Create(
                BuiltInFunctions.Scalars.Id,
                relationship.GetRequiredSymbolicName());
        }

        public static FunctionInvocation Keys(Node node)
        {

            Ensure.IsNotNull(node, "The node parameter is required.");
            return Keys(node.GetRequiredSymbolicName());
        }

        public static FunctionInvocation Keys(Relationship relationship)
        {

            Ensure.IsNotNull(relationship, "The relationship parameter is required.");
            return Keys(relationship.GetRequiredSymbolicName());
        }

        public static FunctionInvocation Keys(Expression expression)
        {

            Ensure.IsNotNull(expression, "The expression parameter is required.");

            Expression param = expression is INamed ? ((INamed) expression).GetRequiredSymbolicName() : expression;
            return FunctionInvocation.Create(BuiltInFunctions.Lists.Keys, param);
        }

        public static FunctionInvocation Exists(Expression expression)
        {
            return FunctionInvocation.Create(BuiltInFunctions.Predicates.Exists, expression);
        }

        public static FunctionInvocation All(Expression expression)
        {
            return FunctionInvocation.Create(BuiltInFunctions.Predicates.All, expression);
        }

    }
}
