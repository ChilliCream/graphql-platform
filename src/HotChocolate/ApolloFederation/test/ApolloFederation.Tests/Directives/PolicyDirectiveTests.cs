using System.Linq;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;

namespace HotChocolate.ApolloFederation.Directives;

public class PolicyDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public void PolicyDirectives_GetParsedCorrectly_SchemaFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                """
                    type Review @policy(policies: [["p3"]]) {
                        id: Int!
                    }

                    type Query {
                        someField(a: Int): Review @policy(policies: [["p1", "p1_1"], ["p2"]])
                    }
                """)
            .AddDirectiveType<PolicyDirectiveType>()
            .Use(_ => _ => default)
            .Create();

        CheckReviewType(schema);
        CheckQueryType(schema);

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void PolicyDirectives_GetAddedCorrectly_Annotations()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        CheckReviewType(schema);
        CheckQueryType(schema);

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void PolicyDirectives_GetAddedCorrectly_CodeFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddType(new ObjectType(d =>
            {
                d.Name(nameof(Review));
                d.Field("id").Type<NonNullType<IntType>>();
            }))
            .AddQueryType(new ObjectType(d =>
            {
                d.Name(nameof(Query));
                d.Field("someField")
                    .Type<NonNullType<ObjectType<Review>>>()
                    .Policy(new PolicyCollection
                    {
                        PolicySets = new PolicySet[]
                        {
                            new()
                            {
                                Policies = new Policy[]
                                {
                                    new()
                                    {
                                        Name = "p1",
                                    },
                                    new()
                                    {
                                        Name = "p1_1",
                                    },
                                },
                            },
                            new()
                            {
                                Policies = new Policy[]
                                {
                                    new()
                                    {
                                        Name = "p2",
                                    },
                                },
                            },
                        },
                    });
            }))
            .Create();

        CheckReviewType(schema);
        CheckQueryType(schema);

        schema.ToString().MatchSnapshot();
    }

    private static PolicyCollection GetSinglePoliciesArgument(IDirectiveCollection directives)
    {
        PolicyCollection? result = null;
        Assert.Collection(
            directives,
            t =>
            {
                Assert.Equal(
                    WellKnownTypeNames.PolicyDirective,
                    t.Type.Name);
                Assert.Collection(
                    t.AsSyntaxNode().Arguments,
                    argument =>
                    {
                        Assert.Equal("policies", argument.Name.Value);
                        var listNode = Assert.IsType<ListValueNode>(argument.Value);
                        result = PolicyParsingHelper.ParseNode(listNode);
                    });
            });
        return result!;
    }

    private static void CheckQueryType(ISchema schema)
    {
        var testType = schema.GetType<ObjectType>(nameof(Query));
        var directives = testType
            .Fields
            .Single(f => f.Name == "someField")
            .Directives;
        var policyCollection = GetSinglePoliciesArgument(directives);
        Assert.Collection(policyCollection.PolicySets,
            t1 =>
            {
                var policies = t1.Policies;
                Assert.Equal("p1", policies[0].Name);
                Assert.Equal("p1_1", policies[1].Name);
            },
            t2 =>
            {
                var policies = t2.Policies;
                Assert.Equal("p2", policies[0].Name);
            });
    }

    private static void CheckReviewType(ISchema schema)
    {
        var testType = schema.GetType<ObjectType>(nameof(Review));
        var directives = testType.Directives;
        var policyCollection = GetSinglePoliciesArgument(directives);
        Assert.Collection(policyCollection.PolicySets,
            t1 =>
            {
                var policies = t1.Policies;
                Assert.Equal("p3", policies[0].Name);
            });
    }

    [Fact]
    public void AnnotateProvidesToClassAttributePureCodeFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        Assert.Collection(
            testType.Fields.Single(field => field.Name == "product").Directives,
            providesDirective =>
            {
                Assert.Equal(
                    WellKnownTypeNames.Provides,
                    providesDirective.Type.Name);
                Assert.Equal(
                    "fields",
                    providesDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"name\"",
                    providesDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.ToString().MatchSnapshot();
    }

    public class Query
    {
        [Policy("p1,p1_1", "p2")]
        public Review SomeField(int id) => default!;
    }

    [Policy("p3")]
    public class Review
    {
        public int Id { get; set; }
    }
}
