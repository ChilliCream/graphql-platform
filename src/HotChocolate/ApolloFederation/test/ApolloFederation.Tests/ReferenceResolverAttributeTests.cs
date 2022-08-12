using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Xunit;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class ReferenceResolverAttributeTests
{
    [Fact(Skip = "Needs to be fixed!")]
    public async void InClassRefResolver_PureCodeFirst()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        // act
        var type = schema.GetType<ObjectType>(nameof(InClassRefResolver));

        // assert
        var result = await ResolveRef(schema, type);
        Assert.Equal(
            nameof(InClassRefResolver),
            Assert.IsType<InClassRefResolver>(result).Id);
    }

    [Fact]
    public async void ExternalRefResolver_PureCodeFirst()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalRefResolver));

        // assert
        var result = await ResolveRef(schema, type);

        Assert.Equal(
            nameof(ExternalRefResolver),
            Assert.IsType<ExternalRefResolver>(result).Id);
    }

    [Fact]
    public async void SingleKey_CompiledResolver()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<QueryWithSingleKeyResolver>()
            .Create();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalSingleKeyResolver));

        // assert
        var result = await ResolveRef(schema, type);

        Assert.Equal("abc", Assert.IsType<ExternalSingleKeyResolver>(result).Id);
    }

    [Fact]
    public async void ExternalFields_Set()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<QueryWithExternalField>()
            .Create();

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
    public async void ExternalFields_Not_Set()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<QueryWithExternalField>()
            .Create();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalFields));
        var representation = new ObjectValueNode(new ObjectFieldNode("id", "id_123"));

        // assert
        var result = await ResolveRef(schema, type, representation);

        Assert.Null(Assert.IsType<ExternalFields>(result).Foo);
    }

    [Fact]
    public async void MultiKey_CompiledResolver()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<QueryWithMultiKeyResolver>()
            .Create();

        var type = schema.GetType<ObjectType>(nameof(ExternalMultiKeyResolver));

        // act
        var resultId = await ResolveRef(schema, type, new(new ObjectFieldNode("id", "id_123")));
        var resultSku = await ResolveRef(schema, type, new(new ObjectFieldNode("sku", "sku_123")));

        // assert
        Assert.Equal("id_123", Assert.IsType<ExternalMultiKeyResolver>(resultId).Id);
        Assert.Equal("sku_123", Assert.IsType<ExternalMultiKeyResolver>(resultSku).Sku);
    }

    [Fact]
    public async void ExternalRefResolver_RenamedMethod_PureCodeFirst()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        // act
        var type = schema.GetType<ObjectType>(nameof(ExternalRefResolverRenamedMethod));

        // assert
        var result = await ResolveRef(schema, type);
        Assert.Equal(
            nameof(ExternalRefResolverRenamedMethod),
            Assert.IsType<ExternalRefResolver>(result).Id);
    }

    [Fact]
    public void InClassRefResolver_RenamedMethod_InvalidName_PureCodeFirst()
    {
        // arrange
        void SchemaCreation()
        {
            SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query_InClass_Invalid>()
                .Create();
        }

        // act
        // assert
        Assert.Throws<SchemaException>((Action)SchemaCreation);
    }

    [Fact]
    public void ExternalRefResolver_RenamedMethod_InvalidName_PureCodeFirst()
    {
        // arrange
        void SchemaCreation()
        {
            SchemaBuilder.New()
                .AddApolloFederation()
                .AddQueryType<Query_ExternalClass_Invalid>()
                .Create();
        }

        // act
        // assert
        Assert.Throws<SchemaException>((Action)SchemaCreation);
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

    public class Query_InClass_Invalid
    {
        public InvalidInClassRefResolver InvalidInClassRefResolver { get; set; } = default!;
    }

    public class Query_ExternalClass_Invalid
    {
        public ExternalRefResolver_Invalid ExternalRefResolver_Invalid { get; set; } = default!;
    }

    [ReferenceResolver(EntityResolver = "non-existing-method")]
    public class InvalidInClassRefResolver
    {
        [Key]
        public string? Id { get; set; }
    }

    [ReferenceResolver(
        EntityResolverType = typeof(InvalidExternalRefResolver),
        EntityResolver = "non-existing-method")]
    public class ExternalRefResolver_Invalid
    {
        [Key]
        public string? Id { get; set; }
    }

    public class InvalidExternalRefResolver
    {
        [Key]
        public string? Id { get; set; }
    }

    public class Query
    {
        public InClassRefResolver InClassRefResolver { get; set; } = default!;
        public ExternalRefResolver ExternalRefResolver { get; set; } = default!;
        public ExternalRefResolverRenamedMethod ExternalRefResolverRenamedMethod { get; set; } =
            default!;
    }

    public class QueryWithSingleKeyResolver
    {
        public ExternalSingleKeyResolver ExternalRefResolver { get; set; } = default!;
    }

    public class QueryWithMultiKeyResolver
    {
        public ExternalMultiKeyResolver ExternalRefResolver { get; set; } = default!;
    }

    public class QueryWithExternalField
    {
        public ExternalFields ExternalRefResolver { get; set; } = default!;
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public class InClassRefResolver
    {
        [Key]
        public string? Id { get; set; }

        public Task<InClassRefResolver> GetAsync([LocalState] ObjectValueNode data)
        {
            return Task.FromResult(
                new InClassRefResolver()
                {
                    Id = nameof(InClassRefResolver)
                });
        }
    }

    [ReferenceResolver(EntityResolverType = typeof(ExternalReferenceResolver))]
    public class ExternalRefResolver
    {
        [Key]
        public string Id { get; set; } = default!;
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public class ExternalSingleKeyResolver
    {
        [Key]
        public string Id { get; set; } = default!;

        public static Task<ExternalSingleKeyResolver> GetAsync(string id)
            => Task.FromResult(new ExternalSingleKeyResolver { Id = id });
    }

    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public class ExternalFields
    {
        [Key]
        public string Id { get; set; } = default!;

        [External]
        public string Foo { get; private set; } = default!;

        public static Task<ExternalFields> GetAsync(string id)
            => Task.FromResult(new ExternalFields { Id = id });
    }

    [Key("id")]
    [Key("sku")]

    public class ExternalMultiKeyResolver
    {
        public string Id { get; set; } = default!;

        public string Sku { get; set; } = default!;

        [ReferenceResolver]
        public static Task<ExternalMultiKeyResolver> GetByIdAsync(string id)
            => Task.FromResult(new ExternalMultiKeyResolver { Id = id });

        [ReferenceResolver]
        public static Task<ExternalMultiKeyResolver> GetBySkuAsync(string sku)
            => Task.FromResult(new ExternalMultiKeyResolver { Sku = sku });
    }

    [ReferenceResolver(
        EntityResolverType = typeof(ExternalReferenceResolverRenamedMethod),
        EntityResolver = nameof(ExternalReferenceResolverRenamedMethod.SomeRenamedMethod))]
    public class ExternalRefResolverRenamedMethod
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
                new ExternalRefResolver()
                {
                    Id = nameof(ExternalRefResolverRenamedMethod)
                });
        }
    }

    public static class ExternalReferenceResolver
    {
        public static Task<ExternalRefResolver> GetExternalReferenceResolverAsync(
            [LocalState] ObjectValueNode data)
        {
            return Task.FromResult(
                new ExternalRefResolver()
                {
                    Id = nameof(ExternalRefResolver)
                });
        }
    }

}
