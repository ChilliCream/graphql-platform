using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using MarshmallowPie.GraphQL.DataLoader;
using MarshmallowPie.Repositories;
using HotChocolate.Types.Relay;
using HotChocolate.Types;
using HotChocolate.Resolvers;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;

namespace MarshmallowPie.GraphQL.Resolvers
{
    public class Query
    {
        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Environment> GetEnvironments(
            [Service]IEnvironmentRepository repository) =>
            repository.GetEnvironments();
    }

    public class Mutation
    {
        public async Task<AddSchemaPayload> AddSchemaAsync(
            AddSchemaInput input,
            [Service]ISchemaRepository repository,
            CancellationToken cancellationToken)
        {
            var schema = new Schema(input.Name, input.Description);
            await repository.AddSchemaAsync(schema, cancellationToken);
            return new AddSchemaPayload(schema);
        }
    }

    public class AddSchemaInput
    {
        public string Name { get; set; }

        public string? Description { get; set; }
    }

    public class AddSchemaPayload
    {
        public AddSchemaPayload(Schema schema)
        {
            Schema = schema;
        }

        public Schema Schema { get; }
    }

    public class Noder : INodeResolver
    {
        public Task<object> ResolveAsync(IResolverContext context, object id)
        {
            throw new NotImplementedException();
        }
    }

    public class FOo
        : TypeInitializationInterceptor
    {

        public override void OnAfterRegisterDependencies(
            IInitializationContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            if (definition is ObjectTypeDefinition otd)
            {
                otd.Fields.FirstOrDefault(t => t.Name.Equals("Id"));
            }
        }
    }
}
