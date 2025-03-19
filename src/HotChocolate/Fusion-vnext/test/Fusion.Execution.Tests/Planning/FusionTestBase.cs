using System.Diagnostics.CodeAnalysis;
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
}
