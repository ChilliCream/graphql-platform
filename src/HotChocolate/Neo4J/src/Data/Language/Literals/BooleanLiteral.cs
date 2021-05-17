using System.Globalization;

namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class BooleanLiteral : Literal<bool>
    {
        private BooleanLiteral(bool context) : base(context)
        {
        }

        public static Literal<bool> Of(bool value) => value ? True : False;

        public override string Print() => Content.ToString(CultureInfo.InvariantCulture);

        public static BooleanLiteral True { get; } = new(true);

        public static BooleanLiteral False { get; } = new(false);
    }
}
