using CookieCrumble.Resources;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;

namespace HotChocolate.Data.MongoDb.Projections;

public class MongoDbProjectionParentRequiresTests(MongoResource resource)
    : ProjectionVisitorTestBase
{
    [Fact]
    public async Task Projection_Should_IncludeRequiredMember_When_ObjectFieldRequiresIgnoredMember()
    {
        // arrange
        var executor = CreateSchema(
            CreatePolicies(),
            resource,
            usePaging: true,
            objectType: new ObjectType<Policy>(
                descriptor =>
                {
                    descriptor.Ignore(t => t.ProductId);
                    descriptor
                        .Field("product")
                        .ResolveWith<PolicyResolvers>(t => t.GetProduct(default!));
                }));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                nodes {
                  product {
                    id
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_IncludeReplacedMember_When_ObjectFieldReplacesRequiredMember()
    {
        // arrange
        var executor = CreateSchema(
            CreatePolicies(),
            resource,
            usePaging: true,
            objectType: new ObjectType<Policy>(
                descriptor => descriptor
                    .Field(t => t.ProductId)
                    .Name("product")
                    .ResolveWith<PolicyResolvers>(t => t.GetProduct(default!))));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                nodes {
                  product {
                    id
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_IncludeRequiredMember_When_LeafFieldRequiresIgnoredMember()
    {
        // arrange
        var executor = CreateSchema(
            CreatePolicies(),
            resource,
            objectType: new ObjectType<Policy>(
                descriptor =>
                {
                    descriptor.Ignore(t => t.ProductId);
                    descriptor
                        .Field("productCode")
                        .ResolveWith<PolicyResolvers>(t => t.GetProductCode(default!));
                }));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                policyNumber
                productCode
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_IncludeWholeRequiredObject_When_SubfieldOfObjectIsSelected()
    {
        // arrange
        var executor = CreateSchema(
            CreatePolicies(),
            resource,
            objectType: new ObjectType<Policy>(
                descriptor => descriptor
                    .Field("holderLabel")
                    .ResolveWith<PolicyResolvers>(t => t.GetHolderLabel(default!))));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                holder {
                  name
                }
                holderLabel
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_IncludeRequiredMember_When_RequiringFieldUsesPaging()
    {
        // arrange
        var executor = CreateSchema(
            CreatePolicies(),
            resource,
            objectType: new ObjectType<Policy>(descriptor =>
            {
                descriptor.Ignore(t => t.ProductId);
                descriptor
                    .Field("products")
                    .UsePaging<ObjectType<Product>>(
                        resolvePagingProvider: (_, _, _) => new QueryableCursorPagingProvider())
                    .ResolveWith<PolicyResolvers>(t => t.GetProducts(default!));
            }));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                policyNumber
                products {
                  nodes {
                    id
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_IncludeRequiredMember_When_RequiringFieldUsesProjection()
    {
        // arrange
        var executor = CreateSchema(
            CreatePolicies(),
            resource,
            objectType: new ObjectType<Policy>(descriptor =>
            {
                descriptor.Ignore(t => t.ProductId);
                descriptor
                    .Field("products")
                    .UseProjection()
                    .ResolveWith<PolicyResolvers>(t => t.GetProducts(default!));
            }));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                policyNumber
                products {
                  id
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_IncludeRequiredMember_When_RequiringFieldIsNotProjected()
    {
        // arrange
        var executor = CreateSchema(
            CreatePolicies(),
            resource,
            objectType: new ObjectType<Policy>(descriptor =>
            {
                descriptor.Ignore(t => t.ProductId);
                descriptor
                    .Field("productCode")
                    .IsProjected(false)
                    .ResolveWith<PolicyResolvers>(t => t.GetProductCode(default!));
            }));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                policyNumber
                productCode
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Projection_Should_RespectConditions_When_RequiringFieldIsConditional(bool skip)
    {
        // arrange
        var executor = CreateSchema(
            CreatePolicies(),
            resource,
            objectType: new ObjectType<Policy>(descriptor =>
            {
                descriptor.Ignore(t => t.ProductId);
                descriptor
                    .Field("productCode")
                    .IsProjected(false)
                    .ResolveWith<PolicyResolvers>(t => t.GetProductCode(default!));
            }));

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($skip: Boolean!) {
                      root {
                        policyNumber
                        productCode @skip(if: $skip)
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { ["skip"] = skip })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create(skip.ToString())
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_IncludeNestedRequiredMember_When_BsonNamesDiffer()
    {
        // arrange
        var executor = CreateSchema(
            [
                new PolicyEnvelope
                {
                    Policy = new NestedPolicy
                    {
                        Holder = new Holder { Name = "Ann", Email = "ann@example.com" }
                    }
                }
            ],
            resource,
            objectType: new ObjectType<PolicyEnvelope>(descriptor => descriptor
                .Field(t => t.Policy)
                .Type(new ObjectType<NestedPolicy>(policy =>
                {
                    policy.Ignore(t => t.Holder);
                    policy
                        .Field("holderEmail")
                        .IsProjected(false)
                        .ResolveWith<PolicyResolvers>(t => t.GetHolderEmail(default!));
                }))));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                policy {
                  policyNumber
                  holderEmail
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Projection_Should_PreserveDiscriminator_When_RequiredMemberIsAbstract()
    {
        // arrange
        var executor = CreateSchema(
            [
                new PolymorphicPolicy<AbstractHolder>
                {
                    Holder = new IndividualHolder { Email = "ann@example.com" }
                }
            ],
            resource,
            objectType: new ObjectType<PolymorphicPolicy<AbstractHolder>>(descriptor =>
            {
                descriptor.Ignore(t => t.Holder);
                descriptor
                    .Field("holderEmail")
                    .IsProjected(false)
                    .ResolveWith<PolicyResolvers>(t => t.GetAbstractHolderEmail(default!));
            }));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                holderEmail
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
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

    [Fact]
    public async Task Projection_Should_PreserveCustomDiscriminator_When_RequiredMemberIsInNestedList()
    {
        // arrange
        var discriminatorConvention = new ScalarDiscriminatorConvention("holder_kind");
        BsonClassMap.RegisterClassMap<ConcreteHolder>(classMap =>
        {
            classMap.AutoMap();
            classMap.SetDiscriminatorConvention(discriminatorConvention);
        });
        BsonClassMap.RegisterClassMap<DerivedHolder>(classMap =>
        {
            classMap.AutoMap();
            classMap.SetDiscriminatorConvention(discriminatorConvention);
        });

        var executor = CreateSchema(
            [
                new PolymorphicPolicyEnvelope
                {
                    Policies =
                    [
                        new PolymorphicPolicy<ConcreteHolder>
                        {
                            Holder = new DerivedHolder { Email = "ann@example.com" }
                        }
                    ]
                }
            ],
            resource,
            objectType: new ObjectType<PolymorphicPolicyEnvelope>(descriptor => descriptor
                .Field(t => t.Policies)
                .Type(new ListType(new ObjectType<PolymorphicPolicy<ConcreteHolder>>(policy =>
                {
                    policy.Ignore(t => t.Holder);
                    policy
                        .Field("holderLabel")
                        .IsProjected(false)
                        .ResolveWith<PolicyResolvers>(t => t.GetConcreteHolderLabel(default!));
                })))));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              root {
                policies {
                  holderLabel
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "root": [
                  {
                    "policies": [
                      {
                        "holderLabel": "DerivedHolder: ann@example.com"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    private static Policy[] CreatePolicies() =>
    [
        new()
        {
            PolicyNumber = "1000-0001",
            ProductId = "P-001",
            Holder = new Holder { Name = "Ann", Email = "ann@example.com" }
        },
        new()
        {
            PolicyNumber = "1000-0002",
            ProductId = "P-002",
            Holder = new Holder { Name = "Bob", Email = "bob@example.com" }
        }
    ];

    public class Policy
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string PolicyNumber { get; set; } = null!;

        public string ProductId { get; set; } = null!;

        public Holder Holder { get; set; } = null!;
    }

    public class Holder
    {
        public string Name { get; set; } = null!;

        [BsonElement("email_address")]
        public string Email { get; set; } = null!;
    }

    public record Product(string Id);

    public class PolicyEnvelope
    {
        [BsonId]
        public string Id { get; set; } = "policy-1";

        [BsonElement("policy_data")]
        public NestedPolicy Policy { get; set; } = null!;
    }

    public class NestedPolicy
    {
        public string PolicyNumber { get; set; } = "1000-0001";

        [BsonElement("policy_holder")]
        public Holder Holder { get; set; } = null!;
    }

    public class PolymorphicPolicy<THolder>
    {
        public string Id { get; set; } = "policy-1";

        [BsonElement("policy_holder")]
        public THolder Holder { get; set; } = default!;
    }

    public class PolymorphicPolicyEnvelope
    {
        public string Id { get; set; } = "envelope-1";

        [BsonElement("policy_data")]
        public PolymorphicPolicy<ConcreteHolder>[] Policies { get; set; } = [];
    }

    [BsonKnownTypes(typeof(IndividualHolder))]
    public abstract class AbstractHolder
    {
        [BsonElement("email_address")]
        public string Email { get; set; } = null!;
    }

    public class IndividualHolder : AbstractHolder;

    [BsonKnownTypes(typeof(DerivedHolder))]
    public class ConcreteHolder
    {
        [BsonElement("email_address")]
        public string Email { get; set; } = null!;
    }

    public class DerivedHolder : ConcreteHolder;

    public class PolicyResolvers
    {
        public Product GetProduct([Parent(requires: nameof(Policy.ProductId))] Policy policy)
            => new(policy.ProductId);

        public Product[] GetProducts([Parent(requires: nameof(Policy.ProductId))] Policy policy)
            => [new(policy.ProductId)];

        public string GetProductCode([Parent(requires: nameof(Policy.ProductId))] Policy policy)
            => "code-" + policy.ProductId;

        public string GetHolderLabel([Parent(requires: nameof(Policy.Holder))] Policy policy)
            => $"{policy.Holder.Name} <{policy.Holder.Email}>";

        public string GetHolderEmail([Parent(requires: "Holder { Email }")] NestedPolicy policy)
            => policy.Holder.Email;

        public string GetAbstractHolderEmail(
            [Parent(requires: "Holder { Email }")] PolymorphicPolicy<AbstractHolder> policy)
            => policy.Holder.Email;

        public string GetConcreteHolderLabel(
            [Parent(requires: "Holder { Email }")] PolymorphicPolicy<ConcreteHolder> policy)
            => $"{policy.Holder.GetType().Name}: {policy.Holder.Email}";
    }
}
