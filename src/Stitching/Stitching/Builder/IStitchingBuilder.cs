using System;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface IStitchingBuilder
    {
        IStitchingBuilder AddSchema(string name, Uri uri);

        IStitchingBuilder AddSchema(string name, DocumentNode document);

        IStitchingBuilder AddSchema(string name, ISchema schema);

        IStitchingBuilder AddExtension(DocumentNode document);

    }

    /*

    StichingBuilder.New()
    .AddRemoteSchema(foo)
    .AddExtensionFromFile("Extensions.graphql")
    .AddConflictResolver(types => new ConflictResolution(false))
    .AddFieldSelector((type, field) => false)
    .RenameField("schemaName", new FieldReference("Type", "field"), "newFieldName")
    .Merge();
     */
}
