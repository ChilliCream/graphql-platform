using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Directives.Legacy;

public class KeyDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public async Task AnnotateKeyToObjectTypeCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation10)
            .AddQueryType(o => o
                .Name("Query")
                .Field("someField")
                .Argument("a", a => a.Type<IntType>())
                .Type("TestType")
                .Resolve(_ => new { Id = 1, Name = "bar" })
            )
            .AddObjectType(
                o =>
                {
                    o.Name("TestType")
                        .Key("id");
                    o.Field("id")
                        .Type<IntType>()
                        .Resolve(_ => 1);
                    o.Field("name")
                        .Type<StringType>()
                        .Resolve(_ => "bar");
                })
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("TestType");

        // assert
        var keyDirective = Assert.Single(testType.Directives);
        Assert.Equal(FederationTypeNames.KeyDirective_Name, keyDirective.Type.Name);
        Assert.Equal("fields", keyDirective.AsSyntaxNode().Arguments[0].Name.ToString());
        Assert.Equal("\"id\"", keyDirective.AsSyntaxNode().Arguments[0].Value.ToString());

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateKeyToObjectTypeAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation10)
            .AddQueryType<Query<TestTypeClassDirective>>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("TestTypeClassDirective");

        // assert
        var keyDirective = Assert.Single(testType.Directives);
        Assert.Equal(FederationTypeNames.KeyDirective_Name, keyDirective.Type.Name);
        Assert.Equal("fields", keyDirective.AsSyntaxNode().Arguments[0].Name.ToString());
        Assert.Equal("\"id\"", keyDirective.AsSyntaxNode().Arguments[0].Value.ToString());

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateKeyToClassAttributeAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation10)
            .AddQueryType<Query<TestTypePropertyDirective>>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("TestTypePropertyDirective");

        // assert
        var keyDirective = Assert.Single(testType.Directives);
        Assert.Equal(FederationTypeNames.KeyDirective_Name, keyDirective.Type.Name);
        Assert.Equal("fields", keyDirective.AsSyntaxNode().Arguments[0].Name.ToString());
        Assert.Equal("\"id\"", keyDirective.AsSyntaxNode().Arguments[0].Value.ToString());

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateKeyToClassAttributesAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation10)
            .AddQueryType<Query<TestTypePropertyDirectives>>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("TestTypePropertyDirectives");

        // assert
        var keyDirective = Assert.Single(testType.Directives);
        Assert.Equal(FederationTypeNames.KeyDirective_Name, keyDirective.Type.Name);
        Assert.Equal("fields", keyDirective.AsSyntaxNode().Arguments[0].Name.ToString());
        Assert.Equal("\"id name\"", keyDirective.AsSyntaxNode().Arguments[0].Value.ToString());

        schema.MatchSnapshot();
    }

    public class Query<T>
    {
        // ReSharper disable once InconsistentNaming
        public T someField(int id) => default!;
    }

    [Key("id")]
    public class TestTypeClassDirective
    {
        public int Id { get; set; }
    }

    public class TestTypePropertyDirective
    {
        [Key]
        public int Id { get; set; }
    }

    public class TestTypePropertyDirectives
    {
        [Key]
        public int Id { get; set; }
        [Key]
        public string Name { get; set; } = default!;
    }
}
