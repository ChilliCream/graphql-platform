using CookieCrumble.Resources;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Projections;

public class MongoDbProjectionPolymorphicParentRequiresTests(MongoResource resource)
{
    [Theory]
    [InlineData(false, QuerySource.Collection)]
    [InlineData(true, QuerySource.Collection)]
    [InlineData(false, QuerySource.Find)]
    [InlineData(true, QuerySource.Find)]
    [InlineData(false, QuerySource.Aggregate)]
    [InlineData(true, QuerySource.Aggregate)]
    public async Task Projection_Should_IncludeRequiredMember_When_InterfaceImplementationRenamesMember(
        bool selectId,
        QuerySource source)
    {
        // arrange
        var collection = resource.CreateCollection<InterfacePolicy>();
        var holder = new InterfaceHolder { Email = "ann@example.com" }.ToBsonDocument();
        holder["_t"] = nameof(InterfaceHolder);
        collection.Database
            .GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .InsertOne(
                new BsonDocument { { "_id", "policy-1" }, { "Holder", holder } },
                cancellationToken: TestContext.Current.CancellationToken);

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddMongoDbProjections()
            .AddType(new ObjectType<InterfacePolicy>(descriptor =>
            {
                descriptor.Ignore(t => t.Holder);
                descriptor
                    .Field("holderEmail")
                    .IsProjected(false)
                    .ResolveWith<Resolvers>(t => t.GetEmail(default!));
            }))
            .AddQueryType(descriptor => descriptor
                .Field("root")
                .Type<ListType<ObjectType<InterfacePolicy>>>()
                .UseProjection()
                .Resolve(_ => source switch
                {
                    QuerySource.Find => collection.Find(FilterDefinition<InterfacePolicy>.Empty).AsExecutable(),
                    QuerySource.Aggregate => collection.Aggregate().AsExecutable(),
                    _ => collection.AsExecutable()
                }))
            .ModifyRequestOptions(options => options.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($selectId: Boolean!) {
                      root {
                        id @include(if: $selectId)
                        holderEmail
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { ["selectId"] = selectId })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            selectId
                ? """
                  {
                    "data": {
                      "root": [
                        {
                          "id": "policy-1",
                          "holderEmail": "ann@example.com"
                        }
                      ]
                    }
                  }
                  """
                : """
                  {
                    "data": {
                      "root": [
                        {
                          "holderEmail": "ann@example.com"
                        }
                      ]
                    }
                  }
                  """);
    }

    [Theory]
    [InlineData(false, QuerySource.Collection)]
    [InlineData(true, QuerySource.Collection)]
    [InlineData(false, QuerySource.Find)]
    [InlineData(true, QuerySource.Find)]
    [InlineData(false, QuerySource.Aggregate)]
    [InlineData(true, QuerySource.Aggregate)]
    public async Task Projection_Should_IncludeRequiredMember_When_CollectionUsesBaseSerializer(
        bool selectId,
        QuerySource source)
    {
        // arrange
        var collection = resource.CreateCollection<BasePolicy>();
        collection.InsertOne(
            new DerivedPolicy { ProductId = "P-001" },
            cancellationToken: TestContext.Current.CancellationToken);
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddMongoDbProjections()
            .AddType(new ObjectType<DerivedPolicy>(descriptor =>
            {
                descriptor.Implements<InterfaceType<BasePolicy>>();
                descriptor.Ignore(t => t.ProductId);
                descriptor
                    .Field("productCode")
                    .IsProjected(false)
                    .ResolveWith<Resolvers>(t => t.GetProductCode(default!));
            }))
            .AddQueryType(descriptor => descriptor
                .Field("root")
                .Type<ListType<InterfaceType<BasePolicy>>>()
                .UseProjection()
                .Resolve(_ => source switch
                {
                    QuerySource.Find => collection.Find(FilterDefinition<BasePolicy>.Empty).AsExecutable(),
                    QuerySource.Aggregate => collection.Aggregate().AsExecutable(),
                    _ => collection.AsExecutable()
                }))
            .ModifyRequestOptions(options => options.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($selectId: Boolean!) {
                      root {
                        id @include(if: $selectId)
                        ... on DerivedPolicy {
                          productCode
                        }
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { ["selectId"] = selectId })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            selectId
                ? """
                  {
                    "data": {
                      "root": [
                        {
                          "id": "policy-1",
                          "productCode": "code-P-001"
                        }
                      ]
                    }
                  }
                  """
                : """
                  {
                    "data": {
                      "root": [
                        {
                          "productCode": "code-P-001"
                        }
                      ]
                    }
                  }
                  """);
    }

    public enum QuerySource
    {
        Collection,
        Find,
        Aggregate
    }

    public class InterfacePolicy
    {
        public string Id { get; set; } = "policy-1";

        public IHolder Holder { get; set; } = null!;
    }

    public interface IHolder
    {
        string Email { get; set; }
    }

    public class InterfaceHolder : IHolder
    {
        [BsonElement("email_address")]
        public string Email { get; set; } = null!;
    }

    [BsonKnownTypes(typeof(DerivedPolicy))]
    public abstract class BasePolicy
    {
        public string Id { get; set; } = "policy-1";
    }

    public class DerivedPolicy : BasePolicy
    {
        [BsonElement("product_id")]
        public string ProductId { get; set; } = null!;
    }

    public class Resolvers
    {
        public string GetEmail([Parent(requires: "Holder { Email }")] InterfacePolicy policy)
            => policy.Holder.Email;

        public string GetProductCode([Parent(requires: "ProductId")] DerivedPolicy policy)
            => "code-" + policy.ProductId;
    }
}
