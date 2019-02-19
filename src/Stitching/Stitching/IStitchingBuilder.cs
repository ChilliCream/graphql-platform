using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    public delegate DocumentNode LoadSchemaDocument(IServiceProvider services);

    public interface IStitchingBuilder
    {
        IStitchingBuilder AddSchema(
            NameString name,
            LoadSchemaDocument loadSchema);

        IStitchingBuilder AddExtensions(
            LoadSchemaDocument loadExtensions);

        IStitchingBuilder AddMergeHandler(
            MergeTypeHandler handler);

        IStitchingBuilder AddSchemaConfiguration(
            Action<ISchemaConfiguration> configure);

        IStitchingBuilder AddExecutionConfiguration(
            Action<IQueryExecutionBuilder> configure);

        IStitchingBuilder SetExecutionOptions(
            IQueryExecutionOptionsAccessor options);

        IStitchingBuilder AddRewriter(ITypeRewriter rewriter);

        IStitchingBuilder AddRewriter(IDocumentRewriter rewriter);
    }
}
