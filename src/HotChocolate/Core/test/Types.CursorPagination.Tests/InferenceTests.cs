using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class InferenceTests
{
    [Fact]
    public async Task Handle_FactoryTypeReference()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddInterfaceType<ProductBase>()
                .AddObjectType<Product>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    public class Query
    {
        [Helper]
        [UsePaging]
        public Task<Connection<ProductBase>> GetProductsAsync()
            => throw new NotImplementedException();
    }

    public abstract class ProductBase
    {
        public string Name { get; set; } = null!;
    }

    public class Product : ProductBase;

    public class Helper : ObjectFieldDescriptorAttribute
    {
        public Helper([CallerLineNumber] int order = 0)
        {
            Order = order;
        }

        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            descriptor.ExtendWith(static extension =>
            {
                var typeRef = extension.Context.TypeInspector.GetTypeRef(
                    typeof(Connection<ProductBase>),
                    TypeContext.Output);
                var factoryTypeRef = TypeReference.Create(typeRef, static (_, type) => type, "SomeKey");
                extension.Configuration.Type = factoryTypeRef;
            });
        }
    }
}
