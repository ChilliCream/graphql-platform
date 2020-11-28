using System.Globalization;

namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class BooleanLiteral : Literal<bool>
    {
        public static readonly BooleanLiteral True = new BooleanLiteral(true);
        public static readonly BooleanLiteral False = new BooleanLiteral(false);

        private BooleanLiteral(bool context) : base(context) { }

        public static Literal<bool> Of(bool value)
        {
            if (value)
            {
                return True;
            }
            else
            {
                return False;
            }
        }

        public override string AsString() => GetContent().ToString(CultureInfo.InvariantCulture);
    }
}
