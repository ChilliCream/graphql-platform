using Microsoft.Extensions.DependencyInjection;
using HotChocolate.ApolloFederation.Support;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.Directives;

public class RequiresScopesDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public async Task RequiresScopesDirectives_GetAddedCorrectly_Annotations()
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
    public async Task RequiresScopesDirectives_GetAddedCorrectly_CodeFirst()
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
                .RequiresScopes(["s1,s1_1", "s2"])
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

    private static string[][] GetSingleRequiresScopesArgument(IDirectiveCollection directives)
    {
        foreach (var directive in directives)
        {
            if (directive.Type.Name != FederationTypeNames.RequiresScopesDirective_Name)
            {
                continue;
            }

            var argument = directive.AsSyntaxNode().Arguments.Single();
            return ParsingHelper.ParseRequiresScopesDirectiveNode(argument.Value);
        }

        Assert.Fail("No requires scopes directive found.");
        return null!;
    }

    private static void CheckQueryType(ISchema schema)
    {
        var testType = schema.GetType<ObjectType>(nameof(Query));
        var directives = testType
            .Fields
            .Single(f => f.Name == "someField")
            .Directives;
        var requiresCollection = GetSingleRequiresScopesArgument(directives);

        Assert.Collection(
            requiresCollection,
            t1 =>
            {
                Assert.Equal("s1", t1[0]);
                Assert.Equal("s1_1", t1[1]);
            },
            t2 => Assert.Equal("s2", t2[0]));
    }

    private static void CheckReviewType(ISchema schema)
    {
        var testType = schema.GetType<ObjectType>(nameof(Review));
        var directives = testType.Directives;
        var requiresCollection = GetSingleRequiresScopesArgument(directives);
        var t1 = Assert.Single(requiresCollection);
        Assert.Equal("s3", t1[0]);
    }

    [Fact]
    public async Task RequiresScopesDirective_GetsAddedCorrectly_Annotations()
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
        [RequiresScopes(["s1, s1_1", "s2"])]
        public Review SomeField(int id) => default!;
    }

    [Key("id")]
    [RequiresScopes(["s3"])]
    public class Review
    {
        public int Id { get; set; }
    }
}
