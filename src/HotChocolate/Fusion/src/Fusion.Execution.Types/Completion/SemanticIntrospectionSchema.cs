using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Completion;

internal static class SemanticIntrospectionSchema
{
    private static DocumentNode? s_document;

    public static ReadOnlySpan<byte> SourceText =>
        """
        type __SearchResult {
          cursor: String!
          coordinate: String!
          definition: __SchemaDefinition!
          pathsToRoot: [[String!]!]!
          score: Float
        }

        union __SchemaDefinition = __Type | __Field | __InputValue | __EnumValue | __Directive
        """u8;

    public static DocumentNode Document
    {
        get
        {
            return s_document ??= Utf8GraphQLParser.Parse(SourceText);
        }
    }
}
