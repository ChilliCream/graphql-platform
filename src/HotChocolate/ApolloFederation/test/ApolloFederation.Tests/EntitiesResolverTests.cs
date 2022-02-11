using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.ApolloFederation.Helpers;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Xunit;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class EntitiesResolverTests
{
    [Fact]
    public async void TestResolveViaForeignServiceType()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("ForeignType", new ObjectValueNode(
                new ObjectFieldNode("id", "1"),
                new ObjectFieldNode("someExternalField", "someExternalField")))
        };

        // assert
        List<object?> result = await EntitiesResolver.ResolveAsync(schema, representations, context);
        ForeignType obj = Assert.IsType<ForeignType>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal("someExternalField", obj.SomeExternalField);
        Assert.Equal("InternalValue", obj.InternalField);
    }

    [Fact]
    public async void TestResolveViaForeignServiceType_MixedTypes()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("MixedFieldTypes",new ObjectValueNode(
                new ObjectFieldNode("id", "1"),
                new ObjectFieldNode("intField", 25)))
        };

        // assert
        List<object?> result = await EntitiesResolver.ResolveAsync(schema, representations, context);
        MixedFieldTypes obj = Assert.IsType<MixedFieldTypes>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal(25, obj.IntField);
        Assert.Equal("InternalValue", obj.InternalField);
    }

    [Fact]
    public async void TestResolveViaEntityResolver()
    {
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("TypeWithReferenceResolver", new ObjectValueNode(new ObjectFieldNode("Id", "1")))
        };

        // assert
        List<object?> result = await EntitiesResolver.ResolveAsync(schema, representations, context);
        TypeWithReferenceResolver obj = Assert.IsType<TypeWithReferenceResolver>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal("SomeField", obj.SomeField);
    }

    [Fact]
    public async void TestResolveViaEntityResolver_NoTypeFound()
    {
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("NonExistingTypeName", new ObjectValueNode())
        };

        // assert
        Task ShouldThrow() => EntitiesResolver.ResolveAsync(schema, representations, context);
        await Assert.ThrowsAsync<SchemaException>(ShouldThrow);
    }

    [Fact]
    public async void TestResolveViaEntityResolver_NoResolverFound()
    {
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        IResolverContext context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("TypeWithoutRefResolver", new ObjectValueNode())
        };

        // assert
        Task ShouldThrow() => EntitiesResolver.ResolveAsync(schema, representations, context);
        await Assert.ThrowsAsync<SchemaException>(ShouldThrow);
    }

    public class Query
    {
        public ForeignType ForeignType { get; set; } = default!;
        public TypeWithReferenceResolver TypeWithReferenceResolver { get; set; } = default!;
        public TypeWithoutRefResolver TypeWithoutRefResolver { get; set; } = default!;
        public MixedFieldTypes MixedFieldTypes { get; set; } = default!;
    }

    public class TypeWithoutRefResolver
    {
        public string Id { get; set; } = default!;
    }

    [ReferenceResolver(EntityResolver = nameof(Get))]
    public class TypeWithReferenceResolver
    {
        public string Id { get; set; } = default!;
        public string SomeField { get; set; } = default!;

        public static TypeWithReferenceResolver Get([LocalState] ObjectValueNode data)
        {
            return new TypeWithReferenceResolver
            {
                Id = "1",
                SomeField = "SomeField"
            };
        }
    }

    [ExtendServiceType]
    public class ForeignType
    {
        public ForeignType(string id, string someExternalField)
        {
            Id = id;
            SomeExternalField = someExternalField;
        }

        [Key]
        [External]
        public string Id { get; }

        [External]
        public string SomeExternalField { get; }

        public string InternalField => "InternalValue";

        [ReferenceResolver]
        public static ForeignType GetById(string id, string someExternalField)
            => new(id, someExternalField);
    }

    [ExtendServiceType]
    public class MixedFieldTypes
    {
        public MixedFieldTypes(string id, int intField)
        {
            Id = id;
            IntField = intField;
        }

        [Key]
        [External]
        public string Id { get; }

        [External]
        public int IntField { get; }

        public string InternalField { get; set; } = "InternalValue";

        [ReferenceResolver]
        public static MixedFieldTypes GetByExternal(string id, int intField) => new(id, intField);
    }
}
