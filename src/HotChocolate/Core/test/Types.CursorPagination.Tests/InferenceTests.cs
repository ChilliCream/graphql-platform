using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class InferenceTests
{
    [Fact]
    public async Task Handle_FactoryTypeReference_For_Connection()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query1>()
                .AddInterfaceType<ProductBase>()
                .AddObjectType<Product>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Handle_FactoryTypeReference_For_Enumerable()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query1>()
                .AddInterfaceType<ProductBase>()
                .AddObjectType<Product>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    public class Query1
    {
        [Helper1]
        [UsePaging]
        public Task<Connection<ProductBase>> GetProductsAsync()
            => throw new NotImplementedException();
    }

    public class Query2
    {
        [Helper1]
        [UsePaging]
        public Task<IEnumerable<ProductBase>> GetProductsAsync()
            => throw new NotImplementedException();
    }

    public abstract class ProductBase
    {
        public string Name { get; set; } = null!;
    }

    public class Product : ProductBase;

    public class Helper1Attribute : ObjectFieldDescriptorAttribute
    {
        public Helper1Attribute([CallerLineNumber] int order = 0)
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
                    typeof(ProductBase),
                    TypeContext.Output);
                var factoryTypeRef = TypeReference.Create(
                    typeRef,
                    static (_, type) => new NonNullType(new ListType(new NonNullType(type))),
                    "SomeKey");
                extension.Configuration.Type = factoryTypeRef;
            });
        }
    }
}
