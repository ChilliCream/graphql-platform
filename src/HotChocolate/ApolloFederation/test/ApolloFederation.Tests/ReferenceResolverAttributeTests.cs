using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.ApolloFederation.FederationContextData;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class ReferenceResolverAttributeTests
{
    [Fact(Skip = "Needs to be fixed!")]
    public async Task InClassRefResolver_PureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        var type = schema.GetType<ObjectType>(nameof(InClassRefResolver));

        // assert
        var result = await ResolveRef(schema, type);
        Assert.Equal(
            nameof(InClassRefResolver),
            Assert.IsType<InClassRefResolver>(result).Id);
    }

    [Fact]
    public async Task ExternalRefResolver_PureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalRefResolver));

        // assert
        var result = await ResolveRef(schema, type);

        Assert.Equal(
            nameof(ExternalRefResolver),
            Assert.IsType<ExternalRefResolver>(result).Id);
    }

    [Fact]
    public async Task SingleKey_CompiledResolver()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<QueryWithSingleKeyResolver>()
            .BuildSchemaAsync();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalSingleKeyResolver));

        // assert
        var result = await ResolveRef(schema, type);

        Assert.Equal("abc", Assert.IsType<ExternalSingleKeyResolver>(result).Id);
    }

    [Fact]
    public async Task ExternalFields_Set()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<QueryWithExternalField>()
            .BuildSchemaAsync();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalFields));
        var representation = new ObjectValueNode(
            new ObjectFieldNode("id", "id_123"),
            new ObjectFieldNode("foo", "bar"));

        // assert
        var result = await ResolveRef(schema, type, representation);

        Assert.Equal("bar", Assert.IsType<ExternalFields>(result).Foo);
    }

    [Fact]
    public async Task ExternalFields_Not_Set()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<QueryWithExternalField>()
            .BuildSchemaAsync();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalFields));
        var representation = new ObjectValueNode(new ObjectFieldNode("id", "id_123"));

        // assert
        var result = await ResolveRef(schema, type, representation);

        Assert.Null(Assert.IsType<ExternalFields>(result).Foo);
    }

    [Fact]
    public async Task MultiKey_CompiledResolver()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<QueryWithMultiKeyResolver>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>(nameof(ExternalMultiKeyResolver));

        // act
        var resultId = await ResolveRef(schema, type, new(new ObjectFieldNode("id", "id_123")));
        var resultSku = await ResolveRef(schema, type, new(new ObjectFieldNode("sku", "sku_123")));

        // assert
        Assert.Equal("id_123", Assert.IsType<ExternalMultiKeyResolver>(resultId).Id);
        Assert.Equal("sku_123", Assert.IsType<ExternalMultiKeyResolver>(resultSku).Sku);
    }

    [Fact]
    public async Task ExternalRefResolver_RenamedMethod_PureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalRefResolverRenamedMethod));

        // assert
        var result = await ResolveRef(schema, type);
        Assert.Equal(
            nameof(ExternalRefResolverRenamedMethod),
            Assert.IsType<ExternalRefResolver>(result).Id);
    }

    [Fact]
    public async Task InClassRefResolver_RenamedMethod_InvalidName_PureCodeFirst()
    {
        // arrange
        async Task SchemaCreation()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddApolloFederation()
                .AddQueryType<Query_InClass_Invalid>()
                .BuildSchemaAsync();
        }

        // act
        // assert
        await Assert.ThrowsAsync<SchemaException>(SchemaCreation);
    }

    [Fact]
    public async Task ExternalRefResolver_RenamedMethod_InvalidName_PureCodeFirst()
    {
        // arrange
        async Task SchemaCreation()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddApolloFederation()
                .AddQueryType<Query_ExternalClass_Invalid>()
                .BuildSchemaAsync();
        }

        // act
        // assert
        await Assert.ThrowsAsync<SchemaException>(SchemaCreation);
    }

    [Fact]
    public async Task InClassRefResolver_WithGuid()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddType<Product>()
            .AddQueryType()
            .BuildSchemaAsync();

        // act
        var result = await schema.MakeExecutable().ExecuteAsync(
            """
            query {
                _entities(representations: [
                    { id: "00000000-0000-0000-0000-000000000000", __typename: "Product" }
                ]) { ... on Product { id } }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    private ValueTask<object?> ResolveRef(ISchema schema, ObjectType type)
        => ResolveRef(schema, type, new ObjectValueNode(new ObjectFieldNode("id", "abc")));

    private async ValueTask<object?> ResolveRef(
        ISchema schema,
        ObjectType type,
        ObjectValueNode representation)
    {
        var inClassResolverContextObject = type.ContextData[EntityResolver];
        Assert.NotNull(inClassResolverContextObject);
        var inClassResolverDelegate =
            Assert.IsType<FieldResolverDelegate>(inClassResolverContextObject);
        var context = CreateResolverContext(schema, type);

        context.SetLocalState(DataField, representation);
        context.SetLocalState(TypeField, type);

        var entity = await inClassResolverDelegate.Invoke(context);

        if (entity is not null &&
            type!.ContextData.TryGetValue(ExternalSetter, out var value) &&
            value is Action<ObjectType, IValueNode, object> setExternals)
        {
            setExternals(type, representation!, entity);
        }

        return entity;
    }

    public sealed class Query_InClass_Invalid
    {
        public InvalidInClassRefResolver InvalidInClassRefResolver { get; set; } = default!;
    }

    public sealed class Query_ExternalClass_Invalid
    {
        public ExternalRefResolver_Invalid ExternalRefResolver_Invalid { get; set; } = default!;
    }

    [ReferenceResolver(EntityResolver = "non-existing-method")]
    public sealed class InvalidInClassRefResolver
    {
        [Key]
        public string? Id { get; set; }
    }

    [ReferenceResolver(
        EntityResolverType = typeof(InvalidExternalRefResolver),
        EntityResolver = "non-existing-method")]
    public sealed class ExternalRefResolver_Invalid
    {
        [Key]
        public string? Id { get; set; }
    }

    public sealed class InvalidExternalRefResolver
    {
        [Key]
        public string? Id { get; set; }
    }

    public sealed class Query
    {
        public InClassRefResolver InClassRefResolver { get; set; } = default!;
        public ExternalRefResolver ExternalRefResolver { get; set; } = default!;
        public ExternalRefResolverRenamedMethod ExternalRefResolverRenamedMethod { get; set; } =
            default!;
    }

    public sealed class QueryWithSingleKeyResolver
    {
        public ExternalSingleKeyResolver ExternalRefResolver { get; set; } = default!;
    }

    public sealed class QueryWithMultiKeyResolver
    {
        public ExternalMultiKeyResolver ExternalRefResolver { get; set; } = default!;
    }

    public sealed class QueryWithExternalField
    {
        public ExternalFields ExternalRefResolver { get; set; } = default!;
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class InClassRefResolver
    {
        [Key]
        public string? Id { get; set; }

        public Task<InClassRefResolver> GetAsync([LocalState] ObjectValueNode data)
        {
            return Task.FromResult(
                new InClassRefResolver
                {
                    Id = nameof(InClassRefResolver),
                });
        }
    }

    [ReferenceResolver(EntityResolverType = typeof(ExternalReferenceResolver))]
    public sealed class ExternalRefResolver
    {
        [Key]
        public string Id { get; set; } = default!;
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class ExternalSingleKeyResolver
    {
        [Key]
        public string Id { get; set; } = default!;

        public static Task<ExternalSingleKeyResolver> GetAsync(string id)
            => Task.FromResult(new ExternalSingleKeyResolver { Id = id, });
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public sealed class ExternalFields
    {
        [Key]
        public string Id { get; set; } = default!;

        [External]
        public string Foo { get; private set; } = default!;

        public static Task<ExternalFields> GetAsync(string id)
            => Task.FromResult(new ExternalFields { Id = id, });
    }

    [Key("id")]
    [Key("sku")]
    public sealed class ExternalMultiKeyResolver
    {
        public string Id { get; set; } = default!;

        public string Sku { get; set; } = default!;

        [ReferenceResolver]
        public static Task<ExternalMultiKeyResolver> GetByIdAsync(string id)
            => Task.FromResult(new ExternalMultiKeyResolver { Id = id, });

        [ReferenceResolver]
        public static Task<ExternalMultiKeyResolver> GetBySkuAsync(string sku)
            => Task.FromResult(new ExternalMultiKeyResolver { Sku = sku, });
    }

    [ReferenceResolver(
        EntityResolverType = typeof(ExternalReferenceResolverRenamedMethod),
        EntityResolver = nameof(ExternalReferenceResolverRenamedMethod.SomeRenamedMethod))]
    public sealed class ExternalRefResolverRenamedMethod
    {
        [Key]
        public string Id { get; set; } = default!;
    }

    public static class ExternalReferenceResolverRenamedMethod
    {
        public static Task<ExternalRefResolver> SomeRenamedMethod(
            [LocalState] ObjectValueNode data)
        {
            return Task.FromResult(
                new ExternalRefResolver
                {
                    Id = nameof(ExternalRefResolverRenamedMethod),
                });
        }
    }

    public static class ExternalReferenceResolver
    {
        public static Task<ExternalRefResolver> GetExternalReferenceResolverAsync(
            [LocalState] ObjectValueNode data)
        {
            return Task.FromResult(
                new ExternalRefResolver
                {
                    Id = nameof(ExternalRefResolver),
                });
        }
    }

    public sealed class Product
    {
        [Key]
        [GraphQLType<NonNullType<IdType>>]
        public Guid Id { get; set; }

        [ReferenceResolver]
        public static Product ResolveProduct(Guid id)
        {
            return new Product
            {
                Id = id
            };
        }
    }
}
