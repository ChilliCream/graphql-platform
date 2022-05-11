using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.ApolloFederation.Helpers;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
            new("ForeignType",
                new ObjectValueNode(
                    new ObjectFieldNode("id", "1"),
                    new ObjectFieldNode("someExternalField", "someExternalField")))
        };

        // assert
        IReadOnlyList<object?> result =
            await EntitiesResolver.ResolveAsync(schema, representations, context);
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
            new("MixedFieldTypes",
                new ObjectValueNode(
                    new ObjectFieldNode("id", "1"),
                    new ObjectFieldNode("intField", 25)))
        };

        // assert
        IReadOnlyList<object?> result =
            await EntitiesResolver.ResolveAsync(schema, representations, context);
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
            new("TypeWithReferenceResolver",
                new ObjectValueNode(new ObjectFieldNode("Id", "1")))
        };

        // assert
        IReadOnlyList<object?> result =
            await EntitiesResolver.ResolveAsync(schema, representations, context);
        TypeWithReferenceResolver obj = Assert.IsType<TypeWithReferenceResolver>(result[0]);
        Assert.Equal("1", obj.Id);
        Assert.Equal("SomeField", obj.SomeField);
    }

    [Fact]
    public async void TestResolveViaEntityResolver_WithDataLoader()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        var batchScheduler = new ManualBatchScheduler();
        var dataLoader = new FederatedTypeDataLoader(batchScheduler);

        IResolverContext context = CreateResolverContext(schema,
            null,
            mock =>
            {
                mock.Setup(c => c.Service<FederatedTypeDataLoader>()).Returns(dataLoader);
            });

        var representations = new List<Representation>
        {
            new("FederatedType", new ObjectValueNode(new ObjectFieldNode("Id", "1"))),
            new("FederatedType", new ObjectValueNode(new ObjectFieldNode("Id", "2"))),
            new("FederatedType", new ObjectValueNode(new ObjectFieldNode("Id", "3")))
        };

        // act
        var resultTask = EntitiesResolver.ResolveAsync(schema, representations, context);
        batchScheduler.Dispatch();
        var results = await resultTask;

        // assert
        Assert.Equal(1, dataLoader.TimesCalled);
        Assert.Equal(3, results.Count);
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
        public static async Task<FederatedType> GetById(
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
            DataLoaderOptions? options = null) : base(batchScheduler, options)
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
                ["3"] = new FederatedType {Id = "3", SomeField = "SomeField-3"}
            };

            return Task.FromResult<IReadOnlyDictionary<string, FederatedType>>(result);
        }
    }
}
