namespace HotChocolate.Data.Neo4J.Language
{
    public class RelationshipDirection
    {
        private readonly string _symbolLeft;
        private readonly string _symbolRight;

        public RelationshipDirection(string symbolLeft, string symbolRight)
        {
            _symbolLeft = symbolLeft;
            _symbolRight = symbolRight;
        }

        public string GetLeftSymbol() => _symbolLeft;
        public string GetRightSymbol() => _symbolRight;

        public static RelationshipDirection Outgoing { get; } = new("-", "->");

        public static RelationshipDirection Incoming { get; } = new("<-", "-");

        public static RelationshipDirection None { get; } = new("-", "-");
    }
}
