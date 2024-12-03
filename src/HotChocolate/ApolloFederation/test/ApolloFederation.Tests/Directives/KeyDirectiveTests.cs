using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Directives;

public class KeyDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public async Task AnnotateKeyToObjectTypeCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
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
        Assert.Collection(
            testType.Directives,
            item =>
            {
                Assert.Equal(FederationTypeNames.KeyDirective_Name, item.Type.Name);
                Assert.Equal("fields", item.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", item.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateKeyToInterfaceTypeCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType(o => o
                .Name("Query")
                .Field("someField")
                .Argument("a", a => a.Type<IntType>())
                .Type("ITestType")
                .Resolve(_ => new { Id = 1, Name = "bar" })
            )
            .AddInterfaceType(
                o =>
                {
                    o.Name("ITestType")
                        .Key("id");
                    o.Field("id")
                        .Type<IntType>();
                    o.Field("name")
                        .Type<StringType>();
                })
            .AddObjectType(
                o =>
                {
                    o.Name("TestType")
                        .Implements("ITestType")
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
        var testType = schema.GetType<InterfaceType>("ITestType");

        // assert
        Assert.Collection(
            testType.Directives,
            item =>
            {
                Assert.Equal(FederationTypeNames.KeyDirective_Name, item.Type.Name);
                Assert.Equal("fields", item.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", item.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateKeyToObjectTypeAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<TestTypeClassDirective>>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("TestTypeClassDirective");

        // assert
        Assert.Collection(testType.Directives,
            item =>
            {
                Assert.Equal(FederationTypeNames.KeyDirective_Name, item.Type.Name);
                Assert.Equal("fields", item.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", item.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateKeyToClassAttributeAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<TestTypePropertyDirective>>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("TestTypePropertyDirective");

        // assert
        Assert.Collection(testType.Directives,
            item =>
            {
                Assert.Equal(FederationTypeNames.KeyDirective_Name, item.Type.Name);
                Assert.Equal("fields", item.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", item.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateKeyToClassAttributesAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<TestTypePropertyDirectives>>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("TestTypePropertyDirectives");

        // assert
        Assert.Collection(testType.Directives,
            item =>
            {
                Assert.Equal(FederationTypeNames.KeyDirective_Name, item.Type.Name);
                Assert.Equal("fields", item.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal("\"id name\"", item.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateKeyToInterfaceAttributesAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<TestTypeClassDirective>>()
            .AddInterfaceType<ITestTypeInterfaceDirective>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<InterfaceType>("ITestTypeInterfaceDirective");

        // assert
        Assert.Collection(testType.Directives,
            item =>
            {
                Assert.Equal(FederationTypeNames.KeyDirective_Name, item.Type.Name);
                Assert.Equal("fields", item.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", item.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    public class Query<T>
    {
        // ReSharper disable once InconsistentNaming
        public T someField(int id) => default!;
    }

    [Key("id")]
    public class TestTypeClassDirective : ITestTypeInterfaceDirective
    {
        public int Id { get; set; }
    }

    [Key("id")]
    public interface ITestTypeInterfaceDirective
    {
        int Id { get; }
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
