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

        public class Scalars : FunctionDefinition
        {
            public static readonly Scalars Coalesce = new("coalesce");
            public static readonly Scalars EndNode = new("endNode");
            public static readonly Scalars Head = new("head");
            public static readonly Scalars Id = new("id");
            public static readonly Scalars Last = new("last");

            public static readonly Scalars Properties = new("properties");
            public static readonly Scalars ShortestPath = new("shortestPath");
            public static readonly Scalars Size = new("size");

            public static readonly Scalars StartNode = new("startNode");
            public static readonly Scalars Type = new("type");
            public Scalars(string implementationName) : base(implementationName) { }
        }

        public class Strings : FunctionDefinition
        {
            public static readonly Strings ToLower = new("toLower");

            public Strings(string implementationName) : base(implementationName) { }
        }

        public class Spatials : FunctionDefinition
        {
            public static readonly Spatials Point = new("point");
            public static readonly Spatials Distance = new("distance");

            public Spatials(string implementationName) : base(implementationName) { }
        }

        public class Aggregates : FunctionDefinition
        {
            public static readonly Aggregates Average = new("avg");
            public static readonly Aggregates Collect = new("collect");
            public static readonly Aggregates Count = new("count");
            public static readonly Aggregates Maximum = new("max");
            public static readonly Aggregates Minimum = new("min");

            public Aggregates(string implementationName) : base(implementationName) { }

            public new bool IsAggregate() => true;
        }
    }
}
