using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching
{
    public interface IStitchingBuilder
    {
        IStitchingBuilder AddSchema(NameString name, Func<DocumentNode> loadSchema);

        IStitchingBuilder AddExtensions(Func<DocumentNode> loadExtensions);

        IStitchingBuilder AddMergeHandler(MergeTypeHandler handler);

        void Populate(IServiceCollection services,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options);
    }
}
