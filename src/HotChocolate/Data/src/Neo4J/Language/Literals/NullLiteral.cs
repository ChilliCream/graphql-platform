namespace HotChocolate.Data.Neo4J.Language
{
    public class NullLiteral<T> : Literal<T>
        where T : class
        {
            public static NullLiteral<T> INSTANCE { get; } = new NullLiteral<T>();
            public NullLiteral() : base(null) { }

            public override string AsString() => "NULL";
        }
}