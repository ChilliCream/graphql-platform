namespace HotChocolate.Data.Neo4J.Language
{
    public class Functions
    {
        public static FunctionInvocation id(Node node) {

            Ensure.IsNotNull(node, "The node for id() is required.");

            return FunctionInvocation.Create(BuiltInFunctions.Scalars.Id, node.GetRequiredSymbolicName());
        }

    }
}
