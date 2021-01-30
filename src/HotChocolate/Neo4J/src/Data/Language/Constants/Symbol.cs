namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Symbols used during Cypher rendering.
    /// </summary>
    public static class Symbol
    {
        public const string NodeLabelStart = ":";
        public const string RelationshipTypeStart = ":";
        public const string RelationshipTypeSeperator = "|";
    }
}
