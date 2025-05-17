using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Directives;

public class NonResolvableKeyDirectiveTests : FederationTypesTestBase
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
                    o.Name("TestTypeResolvableKey")
                        .Key("id");
                    o.Field("id")
                        .Type<IntType>()
                        .Resolve(_ => 1);
                    o.Field("name")
                        .Type<StringType>()
                        .Resolve(_ => "bar");
                })
            .AddObjectType(
                o =>
                {
                    o.Name("TestType")
                        .Key("id", resolvable: false);
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
                var syntaxNode = item.AsSyntaxNode();
                Assert.Equal("fields", syntaxNode.Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", syntaxNode.Arguments[0].Value.ToString());
                Assert.Equal("resolvable", syntaxNode.Arguments[1].Name.ToString());
                Assert.Equal("false", syntaxNode.Arguments[1].Value.ToString());
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
                var syntaxNode = item.AsSyntaxNode();
                Assert.Equal("fields", syntaxNode.Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", syntaxNode.Arguments[0].Value.ToString());
                Assert.Equal("resolvable", syntaxNode.Arguments[1].Name.ToString());
                Assert.Equal("false", syntaxNode.Arguments[1].Value.ToString());
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
                var syntaxNode = item.AsSyntaxNode();
                Assert.Equal("fields", syntaxNode.Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", syntaxNode.Arguments[0].Value.ToString());
                Assert.Equal("resolvable", syntaxNode.Arguments[1].Name.ToString());
                Assert.Equal("false", syntaxNode.Arguments[1].Value.ToString());
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
                var syntaxNode = item.AsSyntaxNode();
                Assert.Equal("fields", syntaxNode.Arguments[0].Name.ToString());
                Assert.Equal("\"id name\"", syntaxNode.Arguments[0].Value.ToString());
                Assert.Equal("resolvable", syntaxNode.Arguments[1].Name.ToString());
                Assert.Equal("false", syntaxNode.Arguments[1].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateInconsistentResolvableKeyToClassAttributesAnnotationBased()
    {
        // act
        var ex = await Assert.ThrowsAsync<SchemaException>(
            async () => await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query<TestTypeInconsistentResolvablePropertyDirectives>>()
            .BuildSchemaAsync());

        // assert
        Assert.Collection(ex.Errors,
            item => Assert.Contains(
                "The specified key attributes must share the same resolvable "
                + "values when annotated on multiple fields.", item.Message));
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
                var syntaxNode = item.AsSyntaxNode();
                Assert.Equal("fields", syntaxNode.Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", syntaxNode.Arguments[0].Value.ToString());
                Assert.Equal("resolvable", syntaxNode.Arguments[1].Name.ToString());
                Assert.Equal("false", syntaxNode.Arguments[1].Value.ToString());
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
                        .Key("id", resolvable: false);
                    o.Field("id")
                        .Type<IntType>();
                    o.Field("name")
                        .Type<StringType>();
                })
            .AddObjectType(
                o =>
                {
                    o.Name("TestTypeResolvableKey")
                        .Implements("ITestType")
                        .Key("id");
                    o.Field("id")
                        .Type<IntType>()
                        .Resolve(_ => 1);
                    o.Field("name")
                        .Type<StringType>()
                        .Resolve(_ => "bar");
                })
            .AddObjectType(
                o =>
                {
                    o.Name("TestType")
                        .Implements("ITestType")
                        .Key("id", resolvable: false);
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
                var syntaxNode = item.AsSyntaxNode();
                Assert.Equal("fields", syntaxNode.Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", syntaxNode.Arguments[0].Value.ToString());
                Assert.Equal("resolvable", syntaxNode.Arguments[1].Name.ToString());
                Assert.Equal("false", syntaxNode.Arguments[1].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    public class Query<T>
    {
        // ReSharper disable once InconsistentNaming
        public T someField(int id) => default!;
    }

    [Key("id", resolvable: false)]
    public class TestTypeClassDirective : ITestTypeInterfaceDirective
    {
        public int Id { get; set; }
    }

    [Key("id", resolvable: false)]
    public interface ITestTypeInterfaceDirective
    {
        int Id { get; }
    }

    public class TestTypePropertyDirective
    {
        [Key(null!, resolvable: false)]
        public int Id { get; set; }
    }

    public class TestTypePropertyDirectives
    {
        [Key(null!, resolvable: false)]
        public int Id { get; set; }
        [Key(null!, resolvable: false)]
        public string Name { get; set; } = default!;
    }

    public class TestTypeInconsistentResolvablePropertyDirectives
    {
        [Key(null!, resolvable: false)]
        public int Id { get; set; }
        [Key]
        public string Name { get; set; } = default!;
    }
}
