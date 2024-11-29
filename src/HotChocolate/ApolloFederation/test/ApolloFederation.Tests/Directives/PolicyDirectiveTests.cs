using Microsoft.Extensions.DependencyInjection;
using HotChocolate.ApolloFederation.Support;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.Directives;

public class PolicyDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public async Task PolicyDirectives_GetAddedCorrectly_Annotations()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        CheckReviewType(schema);
        CheckQueryType(schema);

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task PolicyDirectives_GetAddedCorrectly_CodeFirst()
    {
        // arrange
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
                .Policy(["p1,p1_1", "p2"])
                .Resolve(_ => default);
        });

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddType(reviewType)
            .AddQueryType(queryType)
            .BuildSchemaAsync();

        CheckReviewType(schema);
        CheckQueryType(schema);

        schema.MatchSnapshot();
    }

    private static string[][] GetSinglePoliciesArgument(IDirectiveCollection directives)
    {
        foreach (var directive in directives)
        {
            if (directive.Type.Name != FederationTypeNames.PolicyDirective_Name)
            {
                continue;
            }

            var argument = directive.AsSyntaxNode().Arguments.Single();
            return ParsingHelper.ParsePolicyDirectiveNode(argument.Value);
        }

        Assert.Fail("No policy directive found.");
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

        Assert.Collection(
            policyCollection,
            t1 =>
            {
                Assert.Equal("p1", t1[0]);
                Assert.Equal("p1_1", t1[1]);
            },
            t2 => Assert.Equal("p2", t2[0]));
    }

    private static void CheckReviewType(ISchema schema)
    {
        var testType = schema.GetType<ObjectType>(nameof(Review));
        var directives = testType.Directives;
        var policyCollection = GetSinglePoliciesArgument(directives);
        var t1 = Assert.Single(policyCollection);
        Assert.Equal("p3", t1[0]);
    }

    [Fact]
    public async Task PolicyDirective_GetsAddedCorrectly_Annotations()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        CheckQueryType(schema);
        schema.MatchSnapshot();
    }

    public class Query
    {
        [Policy(["p1, p1_1", "p2"])]
        public Review SomeField(int id) => default!;
    }

    [Key("id")]
    [Policy(["p3"])]
    public class Review
    {
        public int Id { get; set; }
    }
}
