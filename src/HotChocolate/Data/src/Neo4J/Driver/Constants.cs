namespace HotChocolate.Data.Neo4J
{
    internal static class Constants
    {
        public static readonly string NodeIdUnspecifiedMessage =
            $"{nameof(NodeIdAttribute)} not specified or the Node Id is null";

        public static class Statement
        {
            public const string GetNode = @"
                MATCH (node)
                WHERE id(node) = $p1
                RETURN node";

            public const string SetNode = @"
                MATCH (node)
                WHERE id(node) = $p1
                SET node = $p2";
        }
    }
}
