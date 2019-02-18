using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Execution;
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

        IStitchingBuilder IgnoreRootTypes();

        IStitchingBuilder IgnoreRootTypes(
            NameString schemaName);

        IStitchingContext IgnoreType(
            NameString typeName);

        IStitchingContext IgnoreType(
            NameString schemaName,
            NameString typeName);

        IStitchingContext IgnoreField(
            NameString schemaName,
            FieldReference field);

        IStitchingContext RenameType(
            NameString schemaName,
            NameString typeName,
            NameString newName);

        IStitchingContext RenameField(
            NameString schemaName,
            FieldReference field,
            NameString newName);
    }
}
