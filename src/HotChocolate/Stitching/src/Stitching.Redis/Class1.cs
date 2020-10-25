using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Redis
{
    public class RedisExecutorOptionsProvider : IRequestExecutorOptionsProvider
    {
        private NameString _schemaName;

        public RedisExecutorOptionsProvider(NameString schemaName)
        {
            _schemaName = schemaName;
        }

        public async ValueTask<IEnumerable<INamedRequestExecutorFactoryOptions>> GetOptionsAsync(
            CancellationToken cancellationToken)
        {
            IEnumerable<RemoteSchemaDefinition> schemaDefinitions =
                await GetSchemaDefinitionsAsync(cancellationToken)
                    .ConfigureAwait(false);

            var list = new List<INamedRequestExecutorFactoryOptions>();
            var serviceCollection = new ServiceCollection();
            IRequestExecutorBuilder builder = serviceCollection.AddGraphQL();

            foreach (RemoteSchemaDefinition schemaDefinition in schemaDefinitions)
            {
                builder.AddRemoteSchema(
                    schemaDefinition.Name,
                    (sp, ct) => new ValueTask<RemoteSchemaDefinition>(schemaDefinition));

                IServiceProvider services = serviceCollection.BuildServiceProvider();
                IRequestExecutorOptionsMonitor optionsMonitor =
                    services.GetRequiredService<IRequestExecutorOptionsMonitor>();

                RequestExecutorFactoryOptions options =
                    await optionsMonitor.GetAsync(schemaDefinition.Name, cancellationToken)
                        .ConfigureAwait(false);
                // list.Add(new NamedRequestExecutorFactoryOptions(schemaDefinition.Name, options));
            }

            return list;
        }

        public IDisposable OnChange(Action<INamedRequestExecutorFactoryOptions> listener)
        {
            throw new NotImplementedException();
        }


        private async ValueTask<IEnumerable<RemoteSchemaDefinition>> GetSchemaDefinitionsAsync(
            CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
