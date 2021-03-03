namespace HotChocolate.Data.Neo4J.Language
{
    public class RelationshipDirection
    {
        public static readonly RelationshipDirection Outgoing = new ("-", "->");
        public static readonly RelationshipDirection Incoming = new ("<-", "-");
        public static readonly RelationshipDirection None = new("-", "-");

        private readonly string _symbolLeft;
        private readonly string _symbolRight;

        private RelationshipDirection(string symbolLeft, string symbolRight)
        {
            _symbolLeft = symbolLeft;
            _symbolRight = symbolRight;
        }

        public string GetLeftSymbol() => _symbolLeft;
        public string GetRightSymbol() => _symbolRight;
    }
}
