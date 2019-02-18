using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Client;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Properties;
using HotChocolate.Stitching.Utilities;

namespace HotChocolate.Stitching
{
    public partial class StitchingBuilder
        : IStitchingBuilder
    {
        private OrderedDictionary<NameString, LoadSchemaDocument> _schemas =
            new OrderedDictionary<NameString, LoadSchemaDocument>();
        private readonly List<LoadSchemaDocument> _extensions =
            new List<LoadSchemaDocument>();
        private readonly List<MergeTypeHandler> _mergeHandlers =
            new List<MergeTypeHandler>();

        public IStitchingBuilder AddSchema(
            NameString name,
            LoadSchemaDocument loadSchema)
        {
            if (loadSchema == null)
            {
                throw new ArgumentNullException(nameof(loadSchema));
            }

            name.EnsureNotEmpty(nameof(name));

            _schemas.Add(name, loadSchema);

            return this;
        }

        public IStitchingBuilder AddExtensions(
            LoadSchemaDocument loadExtensions)
        {
            if (loadExtensions == null)
            {
                throw new ArgumentNullException(nameof(loadExtensions));
            }

            _extensions.Add(loadExtensions);

            return this;
        }

        public IStitchingBuilder AddMergeHandler(MergeTypeHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _mergeHandlers.Add(handler);

            return this;
        }

        public void Populate(
            IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            serviceCollection.TryAddSingleton(services =>
                StitchingFactory.Create(
                    this, services, configure, options));

            serviceCollection.TryAddScoped(services =>
                services.GetRequiredService<StitchingFactory>()
                    .CreateStitchingContext(services));

            if (!serviceCollection.Any(d =>
                d.ImplementationType == typeof(RemoteQueryBatchOperation)))
            {
                serviceCollection.AddScoped<
                    IBatchOperation,
                    RemoteQueryBatchOperation>();
            }

            serviceCollection.TryAddSingleton(services =>
                services.GetRequiredService<StitchingFactory>()
                    .CreateStitchedQueryExecuter());

            serviceCollection.TryAddSingleton(services =>
                services.GetRequiredService<IQueryExecutor>()
                    .Schema);
        }

        public static StitchingBuilder New() => new StitchingBuilder();
    }


}
