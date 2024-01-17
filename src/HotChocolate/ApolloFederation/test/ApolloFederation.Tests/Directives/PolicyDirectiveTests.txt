using System.Linq;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Types;
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
                    type Review @policy(policies: "p3") {
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

        var reviewType = new ObjectType<Review>(d =>
        {
            d.Name(nameof(Review));
            d.Key("id");
            {
                var id = d.Field("id");
                id.Type<NonNullType<IntType>>();
                id.Resolve(_ => default);
            }
        });
        var queryType = new ObjectType(d =>
        {
            d.Name(nameof(Query));
            d.Field("someField")
                .Type(new NonNullType(reviewType))
                .Policy([["p1", "p1_1"], ["p2"]])
                .Resolve(_ => default);
        });

        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddType(reviewType)
            .AddQueryType(queryType)
            .Create();

        CheckReviewType(schema);
        CheckQueryType(schema);

        schema.ToString().MatchSnapshot();
    }

    private static string[][] GetSinglePoliciesArgument(IDirectiveCollection directives)
    {
        foreach (var directive in directives)
        {
            if (directive.Type.Name != WellKnownTypeNames.PolicyDirective)
            {
                continue;
            }

            var argument = directive.AsSyntaxNode().Arguments.Single();
            return PolicyParsingHelper.ParseNode(argument.Value);
        }

        Assert.True(false, "No policy directive found.");
        return null!;
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
    public void PolicyDirective_GetsAddedCorrectly_Annotations()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        // act
        CheckQueryType(schema);

        schema.ToString().MatchSnapshot();
    }

    public class Query
    {
        [Policy("p1,p1_1", "p2")]
        public Review SomeField(int id) => default!;
    }

    [Key("id")]
    [Policy("p3")]
    public class Review
    {
        public int Id { get; set; }
    }
}
