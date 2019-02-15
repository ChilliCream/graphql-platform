using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching
{
    public delegate DocumentNode LoadSchemaDocument(IServiceProvider services);

    public interface IStitchingBuilder
    {
        IStitchingBuilder AddSchema(NameString name, LoadSchemaDocument loadSchema);

        IStitchingBuilder AddExtensions(LoadSchemaDocument loadExtensions);

        IStitchingBuilder AddMergeHandler(MergeTypeHandler handler);
    }
}
