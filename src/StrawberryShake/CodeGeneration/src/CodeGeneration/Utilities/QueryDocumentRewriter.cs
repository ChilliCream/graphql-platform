using HotChocolate;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities;

public static class QueryDocumentRewriter
{
    public static DocumentNode Rewrite(DocumentNode document, ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(schema);

        var current = document;
        current = EntityIdRewriter.Rewrite(current, schema);
        current = TypeNameQueryRewriter.Rewrite(current);
        current = RemoveClientDirectivesRewriter.Rewrite(current!);
        current = FragmentRewriter.Rewrite(current);
        return current;
    }
}
