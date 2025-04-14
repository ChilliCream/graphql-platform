using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public abstract class FusionTestBase
{
    protected static FusionSchemaDefinition CreateSchema()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        return CompositeSchemaBuilder.Create(compositeSchemaDoc);
    }

    protected static FusionSchemaDefinition CreateSchema(
        [StringSyntax("graphql")] string schema)
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(schema);
        return CompositeSchemaBuilder.Create(compositeSchemaDoc);
    }

    public static FusionSchemaDefinition ComposeSchema(
        [StringSyntax("graphql")] params string[] schemas)
    {
        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(schemas, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        var compositeSchemaDoc = result.Value.ToSyntaxNode();
        return CompositeSchemaBuilder.Create(compositeSchemaDoc);
    }
}

