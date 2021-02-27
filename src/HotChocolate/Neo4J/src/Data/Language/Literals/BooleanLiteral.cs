using System.Globalization;

namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class BooleanLiteral : Literal<bool>
    {
        public static readonly BooleanLiteral True = new (true);
        public static readonly BooleanLiteral False = new (false);

        public BooleanLiteral(bool context) : base(context) { }

        public static Literal<bool> Of(bool value) => value ? True : False;

        public override string AsString() => GetContent().ToString(CultureInfo.InvariantCulture);
    }
}
