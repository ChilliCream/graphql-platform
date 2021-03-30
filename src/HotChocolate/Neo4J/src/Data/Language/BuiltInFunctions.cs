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
            public static readonly Strings Left = new("left");
            public static readonly Strings Right = new("right");
            public static readonly Strings Reverse = new("reverse");
            public static readonly Strings Split = new("split");
            public static readonly Strings Replace = new("replace");
            public static readonly Strings ToLower = new("toLower");
            public static readonly Strings ToUpper = new("toUpper");

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

        public class Lists : FunctionDefinition
        {
            public static readonly Lists Keys = new("keys");
            public static readonly Lists Labels = new("labels");
            public static readonly Lists Nodes = new("nodes");
            public static readonly Lists Range = new("range");
            public static readonly Lists Reduce = new("reduce");
            public static readonly Lists Relationships = new("relationships");

            public Lists(string implementationName) : base(implementationName) { }

        }

        public class Temporals : FunctionDefinition
        {
            public static readonly Temporals Date = new("date");
            public static readonly Temporals Datetime = new("datetime");
            public static readonly Temporals LocalDatetime = new("localdatetime");
            public static readonly Temporals Localtime = new("localtime");
            public static readonly Temporals Time = new("time");
            public static readonly Temporals Duration = new("duration");

            public Temporals(string implementationName) : base(implementationName) { }
        }

        public class MathematicalFunctions : FunctionDefinition
        {
            public static readonly MathematicalFunctions Absolute = new("abs");
            public static readonly MathematicalFunctions Ceiling = new("ceil");
            public static readonly MathematicalFunctions Floor = new("floor");
            public static readonly MathematicalFunctions Random = new("rand", 0, 0);
            public static readonly MathematicalFunctions Round = new("rand", 1, 3);
            public static readonly MathematicalFunctions Sign = new("sign");

            private readonly int _minArgs;

            private readonly int _maxArgs;

            public MathematicalFunctions(string implementationName) : base(implementationName) { }

            public MathematicalFunctions(string implementationName, int min, int max) : base(implementationName)
            {
                _minArgs = min;
                _maxArgs = max;
            }

            public int GetMinArgs() => _minArgs;

            public int GetMaxArgs() => _maxArgs;
        }
    }
}
