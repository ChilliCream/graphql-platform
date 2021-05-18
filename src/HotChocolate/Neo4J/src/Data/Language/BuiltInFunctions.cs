namespace HotChocolate.Data.Neo4J.Language
{
    public class BuiltInFunctions
    {
        public class Predicates : FunctionDefinition
        {
            public Predicates(string implementationName) : base(implementationName)
            {
            }

            public static Predicates All { get; } = new("all");

            public static Predicates Any { get; } = new("any");

            public static Predicates Exists { get; } = new("exists");

            public static Predicates None { get; } = new("none");

            public static Predicates Single { get; } = new("single");
        }
    }
}
