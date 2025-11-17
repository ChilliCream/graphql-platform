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
        [Helper1Attribute]
        [UsePaging]
        public Task<Connection<ProductBase>> GetProductsAsync()
            => throw new NotImplementedException();
    }

    public class Query2
    {
        [Helper2]
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
                var factoryTypeRef = TypeReference.Create(typeRef, static (_, type) => type, "SomeKey");
                extension.Configuration.Type = factoryTypeRef;
            });
        }
    }

    public class Helper2Attribute : ObjectFieldDescriptorAttribute
    {
        public Helper2Attribute([CallerLineNumber] int order = 0)
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
                configuration.Type = TypeReference.Create(
                    typeInspector.GetTypeRef(typeof(global::ChilliCream.Cloud.Management.Host.Schemas.SchemaChangeLogEntry), HotChocolate.Types.TypeContext.Output),
                    static (_, type) => new global::HotChocolate.Types.NonNullType(new global::HotChocolate.Types.ListType(new global::HotChocolate.Types.NonNullType(type))),
                    "[global__ChilliCream_Cloud_Management_Host_Schemas_SchemaChangeLogEntry!]!");
                var factoryTypeRef = TypeReference.Create(typeRef, static (_, type) => type, "SomeKey");
                extension.Configuration.Type = factoryTypeRef;
            });
        }
    }
}
