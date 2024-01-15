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
                    .Policy([["p1", "p1_1"], ["p2"]]);
            }))
            .Create();

        CheckReviewType(schema);
        CheckQueryType(schema);

        schema.ToString().MatchSnapshot();
    }

    private static string[][] GetSinglePoliciesArgument(IDirectiveCollection directives)
    {
        string[][]? result = null;
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
        Assert.Collection(policyCollection,
            t1 =>
            {
                Assert.Equal("p1", t1[0]);
                Assert.Equal("p1_1", t1[1]);
            },
            t2 =>
            {
                Assert.Equal("p2", t2[0]);
            });
    }

    private static void CheckReviewType(ISchema schema)
    {
        var testType = schema.GetType<ObjectType>(nameof(Review));
        var directives = testType.Directives;
        var policyCollection = GetSinglePoliciesArgument(directives);
        Assert.Collection(policyCollection,
            t1 =>
            {
                Assert.Equal("p3", t1[0]);
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
