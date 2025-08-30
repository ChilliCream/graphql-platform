using GreenDonut;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class EntitiesResolverForInterfaceTests
{
    [Fact]
    public async Task TestResolveViaForeignServiceType()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<ForeignType>()
            .AddType<TypeWithReferenceResolver>()
            .AddType<TypeWithoutRefResolver>()
            .AddType<MixedFieldTypes>()
            .AddType<FederatedType>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = RepresentationsOf(
            nameof(IForeignType),
            new
            {
                id = "1",
                someExternalField = "someExternalField"
            });
        var result =
            await EntitiesResolver.ResolveAsync(schema, representations, context);

        // assert
        var obj = Assert.IsAssignableFrom<IForeignType>(result[0]);
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
            .AddType<ForeignType>()
            .AddType<TypeWithReferenceResolver>()
            .AddType<TypeWithoutRefResolver>()
            .AddType<MixedFieldTypes>()
            .AddType<FederatedType>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("IMixedFieldTypes",
                new ObjectValueNode(
                    new ObjectFieldNode("id", "1"),
                    new ObjectFieldNode("intField", 25)))
        };

        // assert
        var result =
            await EntitiesResolver.ResolveAsync(schema, representations, context);
        var obj = Assert.IsAssignableFrom<IMixedFieldTypes>(result[0]);
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
            .AddType<ForeignType>()
            .AddType<TypeWithReferenceResolver>()
            .AddType<TypeWithoutRefResolver>()
            .AddType<MixedFieldTypes>()
            .AddType<FederatedType>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("ITypeWithReferenceResolver",
                new ObjectValueNode(new ObjectFieldNode("Id", "1")))
        };

        // assert
        var result = await EntitiesResolver.ResolveAsync(schema, representations, context);
        var obj = Assert.IsAssignableFrom<ITypeWithReferenceResolver>(result[0]);
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
            .AddType<ForeignType>()
            .AddType<TypeWithReferenceResolver>()
            .AddType<TypeWithoutRefResolver>()
            .AddType<MixedFieldTypes>()
            .AddType<FederatedType>()
            .BuildSchemaAsync();

        var batchScheduler = new ManualBatchScheduler();
        var dataLoader = new IFederatedTypeDataLoader(batchScheduler, new DataLoaderOptions());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(c => c.GetService(typeof(IFederatedTypeDataLoader))).Returns(dataLoader);

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
            .AddType<ForeignType>()
            .AddType<TypeWithReferenceResolver>()
            .AddType<TypeWithoutRefResolver>()
            .AddType<MixedFieldTypes>()
            .AddType<FederatedType>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

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
    public async Task TestResolveViaEntityResolver_NoResolverFound()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<ForeignType>()
            .AddType<TypeWithReferenceResolver>()
            .AddType<TypeWithoutRefResolver>()
            .AddType<MixedFieldTypes>()
            .AddType<FederatedType>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        // act
        var representations = new List<Representation>
        {
            new("ITypeWithoutRefResolver", new ObjectValueNode())
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
            .AddType<ForeignType>()
            .AddType<TypeWithReferenceResolver>()
            .AddType<TypeWithoutRefResolver>()
            .AddType<MixedFieldTypes>()
            .AddType<FederatedType>()
            .AddType<IFederatedTypeWithRequiredDetail>()
            .AddType<FederatedTypeWithRequiredDetail>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        var representations = new List<Representation>
        {
            new("IFederatedTypeWithRequiredDetail",
                new ObjectValueNode(
                [
                    new ObjectFieldNode("detail",
                        new ObjectValueNode([new ObjectFieldNode("id", "testId")]))
                ]))
        };

        var result = await EntitiesResolver.ResolveAsync(schema, representations, context);

        var single = Assert.Single(result);
        var obj = Assert.IsAssignableFrom<IFederatedTypeWithRequiredDetail>(single);

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
            .AddType<ForeignType>()
            .AddType<TypeWithReferenceResolver>()
            .AddType<TypeWithoutRefResolver>()
            .AddType<MixedFieldTypes>()
            .AddType<FederatedType>()
            .AddType<IFederatedTypeWithOptionalDetail>()
            .AddType<FederatedTypeWithOptionalDetail>()
            .BuildSchemaAsync();

        var context = CreateResolverContext(schema);

        var representations = new List<Representation>
        {
            new("IFederatedTypeWithOptionalDetail",
                new ObjectValueNode(
                [
                    new ObjectFieldNode("detail",
                        new ObjectValueNode(
                        [
                            new ObjectFieldNode("id", "testId")
                        ]))
                ]))
        };

        var result = await EntitiesResolver.ResolveAsync(schema, representations, context);

        var single = Assert.Single(result);
        var obj = Assert.IsAssignableFrom<IFederatedTypeWithOptionalDetail>(single);

        Assert.Equal("testId", obj.Id);
        Assert.Equal("testId", obj.Detail!.Id);
    }

    public class Query
    {
        public IForeignType ForeignType { get; set; } = null!;
        public ITypeWithReferenceResolver TypeWithReferenceResolver { get; set; } = null!;
        public ITypeWithoutRefResolver TypeWithoutRefResolver { get; set; } = null!;
        public IMixedFieldTypes MixedFieldTypes { get; set; } = null!;
        public IFederatedType TypeWithReferenceResolverMany { get; set; } = null!;
    }

    public interface ITypeWithoutRefResolver
    {
        string Id { get; set; }
    }

    public class TypeWithoutRefResolver : ITypeWithoutRefResolver
    {
        public string Id { get; set; } = null!;
    }

    [ReferenceResolver(EntityResolver = nameof(Get))]
    public interface ITypeWithReferenceResolver
    {
        string Id { get; set; }
        string SomeField { get; set; }

        public static ITypeWithReferenceResolver Get([LocalState] ObjectValueNode data)
        {
            return new TypeWithReferenceResolver { Id = "1", SomeField = "SomeField" };
        }
    }

    [ReferenceResolver(EntityResolver = nameof(Get))]
    public class TypeWithReferenceResolver : ITypeWithReferenceResolver
    {
        public string Id { get; set; } = null!;
        public string SomeField { get; set; } = null!;

        public static TypeWithReferenceResolver Get([LocalState] ObjectValueNode data)
        {
            return new TypeWithReferenceResolver { Id = "1", SomeField = "SomeField" };
        }
    }

    public interface IForeignType
    {
        [Key]
        public string Id { get; }

        [External]
        public string SomeExternalField { get; }

        public string InternalField => "InternalValue";

        [ReferenceResolver]
        public static IForeignType GetById(string id, string someExternalField)
            => new ForeignType(id, someExternalField);
    }

    public class ForeignType : IForeignType
    {
        public ForeignType(string id, string someExternalField)
        {
            Id = id;
            SomeExternalField = someExternalField;
        }

        [Key]
        public string Id { get; }

        [External]
        public string SomeExternalField { get; }

        public string InternalField => "InternalValue";

        [ReferenceResolver]
        public static ForeignType GetById(string id, string someExternalField)
            => new(id, someExternalField);
    }

    public interface IMixedFieldTypes
    {
        [Key]
        public string Id { get; }

        [External]
        public int IntField { get; }

        public string InternalField { get; set; }

        [ReferenceResolver]
        public static IMixedFieldTypes GetByExternal(string id, int intField)
            => new MixedFieldTypes(id, intField);
    }

    [ExtendServiceType]
    public class MixedFieldTypes : IMixedFieldTypes
    {
        public MixedFieldTypes(string id, int intField)
        {
            Id = id;
            IntField = intField;
        }

        [Key]
        public string Id { get; }

        [External]
        public int IntField { get; }

        public string InternalField { get; set; } = "InternalValue";

        [ReferenceResolver]
        public static MixedFieldTypes GetByExternal(string id, int intField) => new(id, intField);
    }

    public interface IFederatedType
    {
        [Key]
        string Id { get; set; }

        string SomeField { get; set; }

        [ReferenceResolver]
        public static async Task<IFederatedType?> GetById(
            [LocalState] ObjectValueNode data,
            [Service] IFederatedTypeDataLoader loader)
        {
            var id =
                data.Fields.FirstOrDefault(_ => _.Name.Value == "Id")?.Value.Value?.ToString() ??
                string.Empty;

            return await loader.LoadAsync(id);
        }
    }

    public class FederatedType : IFederatedType
    {
        [Key]
        public string Id { get; set; } = null!;

        public string SomeField { get; set; } = null!;

        [ReferenceResolver]
        public static async Task<FederatedType?> GetById(
            [LocalState] ObjectValueNode data,
            [Service] IFederatedTypeDataLoader loader)
        {
            var id =
                data.Fields.FirstOrDefault(_ => _.Name.Value == "Id")?.Value.Value?.ToString() ??
                string.Empty;

            return (FederatedType?)await loader.LoadAsync(id);
        }
    }

    public class IFederatedTypeDataLoader : BatchDataLoader<string, IFederatedType>
    {
        public int TimesCalled { get; private set; }

        public IFederatedTypeDataLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions options) : base(batchScheduler, options)
        {
        }

        protected override Task<IReadOnlyDictionary<string, IFederatedType>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            TimesCalled++;

            Dictionary<string, IFederatedType> result = new()
            {
                ["1"] = new FederatedType { Id = "1", SomeField = "SomeField-1" },
                ["2"] = new FederatedType { Id = "2", SomeField = "SomeField-2" },
                ["3"] = new FederatedType { Id = "3", SomeField = "SomeField-3" }
            };

            return Task.FromResult<IReadOnlyDictionary<string, IFederatedType>>(result);
        }
    }

    public interface IFederatedTypeWithRequiredDetail
    {
        string Id { get; set; }

        FederatedTypeDetail Detail { get; set; }

        [ReferenceResolver]
        public static IFederatedTypeWithRequiredDetail ReferenceResolver([Map("detail.id")] string detailId)
            => new FederatedTypeWithRequiredDetail()
            {
                Id = detailId,
                Detail = new FederatedTypeDetail
                {
                    Id = detailId
                }
            };
    }

    public class FederatedTypeWithRequiredDetail : IFederatedTypeWithRequiredDetail
    {
        public string Id { get; set; } = null!;

        public FederatedTypeDetail Detail { get; set; } = null!;

        [ReferenceResolver]
        public static FederatedTypeWithRequiredDetail ReferenceResolver([Map("detail.id")] string detailId)
            => new()
            {
                Id = detailId,
                Detail = new FederatedTypeDetail
                {
                    Id = detailId
                }
            };
    }

    public interface IFederatedTypeWithOptionalDetail
    {
        string Id { get; set; }

        FederatedTypeDetail? Detail { get; }

        [ReferenceResolver]
        public static IFederatedTypeWithOptionalDetail ReferenceResolver([Map("detail.id")] string detailId)
            => new FederatedTypeWithOptionalDetail()
            {
                Id = detailId,
                Detail = new FederatedTypeDetail
                {
                    Id = detailId
                }
            };
    }

    public class FederatedTypeWithOptionalDetail : IFederatedTypeWithOptionalDetail
    {
        public string Id { get; set; } = null!;

        public FederatedTypeDetail? Detail { get; set; }

        [ReferenceResolver]
        public static FederatedTypeWithOptionalDetail ReferenceResolver([Map("detail.id")] string detailId)
            => new()
            {
                Id = detailId,
                Detail = new FederatedTypeDetail
                {
                    Id = detailId
                }
            };
    }

    public class FederatedTypeDetail
    {
        public string Id { get; set; } = null!;
    }
}
