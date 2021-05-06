namespace HotChocolate.Data.Neo4J.Language
{
    public class BuiltInFunctions
    {
        public class Predicates : FunctionDefinition
        {
            public static readonly Predicates All = new("all");
            public static readonly Predicates Any = new("any");
            public static readonly Predicates Exists = new("exists");
            public static readonly Predicates None = new("none");
            public static readonly Predicates Single = new("single");
            public Predicates(string implementationName) : base(implementationName) { }
        }
    }
}
