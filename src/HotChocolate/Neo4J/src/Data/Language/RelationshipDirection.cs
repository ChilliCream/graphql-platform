namespace HotChocolate.Data.Neo4J.Language
{
    public class RelationshipDirection
    {
        public RelationshipDirection(string symbolLeft, string symbolRight)
        {
            LeftSymbol = symbolLeft;
            RightSymbol = symbolRight;
        }

        public string LeftSymbol { get; }

        public string RightSymbol { get; }

        public static RelationshipDirection Outgoing { get; } = new("-", "->");

        public static RelationshipDirection Incoming { get; } = new("<-", "-");

        public static RelationshipDirection None { get; } = new("-", "-");
    }
}
