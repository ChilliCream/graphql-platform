using System.Globalization;

namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class BooleanLiteral : Literal<bool>
    {

        public static readonly BooleanLiteral True = new (true);
        public static readonly BooleanLiteral False = new (false);

        private BooleanLiteral(bool context) : base(context) { }

        public static Literal<bool> Of(bool value)
        {
            return value ? True : False;
        }

        public override string AsString() => GetContent().ToString(CultureInfo.InvariantCulture);

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            visitor.Leave(this);
        }
    }
}
