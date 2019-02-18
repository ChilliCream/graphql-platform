using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;

namespace HotChocolate.Stitching
{
    public delegate DocumentNode LoadSchemaDocument(IServiceProvider services);

    public interface IStitchingBuilder
    {
        IStitchingBuilder AddSchema(
            NameString name,
            LoadSchemaDocument loadSchema);

        IStitchingBuilder AddExtensions(LoadSchemaDocument loadExtensions);

        IStitchingBuilder AddMergeHandler(MergeTypeHandler handler);
    }
}
