namespace HotChocolate.Data.Neo4J.Language
{
    public class BooleanLiteral : Literal<bool>
    {
        public static readonly BooleanLiteral TRUE = new BooleanLiteral(true);
        public static readonly BooleanLiteral FALSE = new BooleanLiteral(false);

        private BooleanLiteral(bool context) : base(context) { }

        static Literal<bool> Of(bool value)
        {
            if (value)
            {
                return TRUE;
            }
            else
            {
                return FALSE;
            }
        }

        public override string AsString() => base.GetContent().ToString();
    }
}