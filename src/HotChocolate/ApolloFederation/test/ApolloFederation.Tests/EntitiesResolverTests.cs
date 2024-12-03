using GreenDonut;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class EntitiesResolverTests
{
    [Fact]
    public async Task TestResolveViaForeignServiceType()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = RepresentationsOf(
            nameof(ForeignType),
            new
            {
                id = "1",
                someExternalField = "someExternalField",
            });
        var result =
            await EntitiesResolver.ResolveAsync(schema, representations, context);

        // assert
        var obj = Assert.IsType<ForeignType>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal("someExternalField", obj.SomeExternalField);
        Assert.Equal("InternalValue", obj.InternalField);
    }

    [Fact]
    public async Task TestResolveViaForeignServiceType_MixedTypes()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("MixedFieldTypes",
                new ObjectValueNode(
                    new ObjectFieldNode("id", "1"),
                    new ObjectFieldNode("intField", 25))),
        };

        // assert
        var result =
            await EntitiesResolver.ResolveAsync(schema, representations, context);
        var obj = Assert.IsType<MixedFieldTypes>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal(25, obj.IntField);
        Assert.Equal("InternalValue", obj.InternalField);
    }

    [Fact]
    public async Task TestResolveViaEntityResolver()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("TypeWithReferenceResolver",
                new ObjectValueNode(new ObjectFieldNode("Id", "1"))),
        };

        // assert
        var result = await EntitiesResolver.ResolveAsync(schema, representations, context);
        var obj = Assert.IsType<TypeWithReferenceResolver>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal("SomeField", obj.SomeField);
    }

    [Fact]
    public async Task TestResolveViaEntityResolver_WithDataLoader()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        var batchScheduler = new ManualBatchScheduler();
        var dataLoader = new FederatedTypeDataLoader(batchScheduler, new DataLoaderOptions());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(c => c.GetService(typeof(FederatedTypeDataLoader))).Returns(dataLoader);

        var context = CreateResolverContext(
            schema,
            null,
            mock => mock.Setup(c => c.Services).Returns(serviceProviderMock.Object));

        var representations = RepresentationsOf(
            nameof(FederatedType),
            new { Id = "1" },
            new { Id = "2" },
            new { Id = "3" });

        // act
        var resultTask = EntitiesResolver.ResolveAsync(schema, representations, context);
        batchScheduler.Dispatch();
        var results = await resultTask;

        // assert
        Assert.Equal(1, dataLoader.TimesCalled);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task TestResolveViaEntityResolver_NoTypeFound()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("NonExistingTypeName", new ObjectValueNode()),
        };

        // assert
        Task ShouldThrow() => EntitiesResolver.ResolveAsync(schema, representations, context);
        await Assert.ThrowsAsync<SchemaException>(ShouldThrow);
    }

    [Fact]
    public async Task TestResolveViaEntityResolver_NoResolverFound()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("TypeWithoutRefResolver", new ObjectValueNode()),
        };

        // assert
        Task ShouldThrow() => EntitiesResolver.ResolveAsync(schema, representations, context);
        await Assert.ThrowsAsync<SchemaException>(ShouldThrow);
    }

    [Fact]
    public async Task TestDetailFieldResolver_Required()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<FederatedTypeWithRequiredDetail>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        var representations = new List<Representation>
        {
            new("FederatedTypeWithRequiredDetail",
                new ObjectValueNode(new[]
                {
                    new ObjectFieldNode("detail",
                        new ObjectValueNode(new[] { new ObjectFieldNode("id", "testId") })),
                })),
        };

        var result = await EntitiesResolver.ResolveAsync(schema, representations, context);

        var single = Assert.Single(result);
        var obj = Assert.IsType<FederatedTypeWithRequiredDetail>(single);

        Assert.Equal("testId", obj.Id);
        Assert.Equal("testId", obj.Detail.Id);
    }

    [Fact]
    public async Task TestDetailFieldResolver_Optional()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<FederatedTypeWithOptionalDetail>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        var representations = new List<Representation>
        {
            new("FederatedTypeWithOptionalDetail",
                new ObjectValueNode(new[]
                {
                    new ObjectFieldNode("detail",
                        new ObjectValueNode(new[]
                        {
                            new ObjectFieldNode("id", "testId"),
                        })),
                })),
        };

        var result = await EntitiesResolver.ResolveAsync(schema, representations, context);

        var single = Assert.Single(result);
        var obj = Assert.IsType<FederatedTypeWithOptionalDetail>(single);

        Assert.Equal("testId", obj.Id);
        Assert.Equal("testId", obj.Detail!.Id);
    }

    public class Query
    {
        public ForeignType ForeignType { get; set; } = default!;
        public TypeWithReferenceResolver TypeWithReferenceResolver { get; set; } = default!;
        public TypeWithoutRefResolver TypeWithoutRefResolver { get; set; } = default!;
        public MixedFieldTypes MixedFieldTypes { get; set; } = default!;
        public FederatedType TypeWithReferenceResolverMany { get; set; } = default!;
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
            return new TypeWithReferenceResolver {Id = "1", SomeField = "SomeField"};
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

    [ExtendServiceType]
    public class FederatedType
    {
        [Key]
        [External]
        public string Id { get; set; } = default!;

        public string SomeField { get; set; } = default!;

        [ReferenceResolver]
        public static async Task<FederatedType?> GetById(
            [LocalState] ObjectValueNode data,
            [Service] FederatedTypeDataLoader loader)
        {
            var id =
                data.Fields.FirstOrDefault(_ => _.Name.Value == "Id")?.Value.Value?.ToString() ??
                string.Empty;

            return await loader.LoadAsync(id);
        }
    }

    public class FederatedTypeDataLoader : BatchDataLoader<string, FederatedType>
    {
        public int TimesCalled { get; private set; }

        public FederatedTypeDataLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions options) : base(batchScheduler, options)
        {
        }

        protected override Task<IReadOnlyDictionary<string, FederatedType>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            TimesCalled++;

            Dictionary<string, FederatedType> result = new()
            {
                ["1"] = new FederatedType {Id = "1", SomeField = "SomeField-1"},
                ["2"] = new FederatedType {Id = "2", SomeField = "SomeField-2"},
                ["3"] = new FederatedType {Id = "3", SomeField = "SomeField-3"},
            };

            return Task.FromResult<IReadOnlyDictionary<string, FederatedType>>(result);
        }
    }

    public class FederatedTypeWithRequiredDetail
    {
        public string Id { get; set; } = default!;

        public FederatedTypeDetail Detail { get; set; } = default!;

        [ReferenceResolver]
        public static FederatedTypeWithRequiredDetail ReferenceResolver([Map("detail.id")] string detailId)
            => new()
            {
                Id = detailId,
                Detail = new FederatedTypeDetail
                {
                    Id = detailId,
                },
            };
    }

    public class FederatedTypeWithOptionalDetail
    {
        public string Id { get; set; } = default!;

        public FederatedTypeDetail? Detail { get; set; } = default!;

        [ReferenceResolver]
        public static FederatedTypeWithOptionalDetail ReferenceResolver([Map("detail.id")] string detailId)
            => new()
            {
                Id = detailId,
                Detail = new FederatedTypeDetail
                {
                    Id = detailId,
                },
            };
    }

    public class FederatedTypeDetail
    {
        public string Id { get; set; } = default!;
    }
}
