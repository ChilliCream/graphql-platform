using CookieCrumble;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizeDirectiveTests
{
    [Fact]
    public void CreateInstance_PolicyRoles_PolicyIsNullRolesHasItems()
    {
        // arrange
        // act
        var authorizeDirective = new AuthorizeDirective(
            null,
            new[] { "a", "b", });

        // assert
        Assert.Null(authorizeDirective.Policy);
        Assert.Collection(
            authorizeDirective.Roles!,
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t));
    }

    [Fact]
    public void CreateInstance_PolicyRoles_PolicyIsSetRolesIsEmpty()
    {
        // arrange
        // act
        var authorizeDirective = new AuthorizeDirective(
            "abc",
            Array.Empty<string>());

        // assert
        Assert.Equal("abc", authorizeDirective.Policy);
        Assert.Empty(authorizeDirective.Roles);
    }

    [Fact]
    public void CreateInstance_PolicyRoles_PolicyIsSetRolesIsNull()
    {
        // arrange
        // act
        var authorizeDirective = new AuthorizeDirective(
            "abc",
            Array.Empty<string>());

        // assert
        Assert.Equal("abc", authorizeDirective.Policy);
        Assert.Empty(authorizeDirective.Roles!);
    }

    [Fact]
    public void CreateInstance_Policy_PolicyIsSet()
    {
        // arrange
        // act
        var authorizeDirective = new AuthorizeDirective("abc");

        // assert
        Assert.Equal("abc", authorizeDirective.Policy);
        Assert.Null(authorizeDirective.Roles);
    }

    [Fact]
    public void CreateInstance_Roles_RolesHasItems()
    {
        // arrange
        // act
        var authorizeDirective = new AuthorizeDirective(
            new[] { "a", "b", });

        // assert
        Assert.Null(authorizeDirective.Policy);
        Assert.Collection(
            authorizeDirective.Roles,
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t));
    }

    [Fact]
    public void TypeAuth_DefaultPolicy()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Authorize(ApplyPolicy.BeforeResolver)
                    .Field("foo")
                    .Resolve("bar"))
            .AddAuthorizeDirectiveType()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void TypeAuth_DefaultPolicy_DescriptorNull()
    {
        // arrange
        // act
        Action action = () =>
            AuthorizeObjectTypeDescriptorExtensions.Authorize(null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void TypeAuth_WithPolicy()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Authorize("MyPolicy", ApplyPolicy.BeforeResolver)
                    .Field("foo")
                    .Resolve("bar"))
            .AddAuthorizeDirectiveType()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void TypeAuth_WithPolicy_DescriptorNull()
    {
        // arrange
        // act
        Action action = () =>
            AuthorizeObjectTypeDescriptorExtensions
                .Authorize(null!, "MyPolicy");

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void TypeAuth_WithRoles()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Authorize(["MyRole",])
                    .Field("foo")
                    .Resolve("bar"))
            .AddAuthorizeDirectiveType()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void TypeAuth_WithRoles_DescriptorNull()
    {
        // arrange
        // act
        void Action()
            => AuthorizeObjectTypeDescriptorExtensions.Authorize(null!, ["MyRole",]);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task FieldAuth_DefaultPolicy()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize(ApplyPolicy.BeforeResolver)
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task FieldAuth_DefaultPolicy_AfterResolver()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize(apply: ApplyPolicy.AfterResolver)
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task FieldAuth_DefaultPolicy_BeforeResolver()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize(apply: ApplyPolicy.BeforeResolver)
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task FieldAuth_DefaultPolicy_Validation()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize(apply: ApplyPolicy.Validation)
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void FieldAuth_DefaultPolicy_DescriptorNull()
    {
        // arrange
        // act
        Action action = () =>
            AuthorizeObjectFieldDescriptorExtensions
                .Authorize(null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task FieldAuth_WithPolicy()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize("MyPolicy", ApplyPolicy.BeforeResolver)
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task FieldAuth_WithPolicy_AfterResolver()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize("MyPolicy", apply: ApplyPolicy.AfterResolver)
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task FieldAuth_WithPolicy_BeforeResolver()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize("MyPolicy", apply: ApplyPolicy.BeforeResolver)
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task FieldAuth_WithPolicy_Validation()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize("MyPolicy", apply: ApplyPolicy.Validation)
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void FieldAuth_WithPolicy_DescriptorNull()
    {
        // arrange
        // act
        Action action = () =>
            AuthorizeObjectFieldDescriptorExtensions
                .Authorize(null!, "MyPolicy");

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task FieldAuth_WithRoles()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddLogging()
                .AddAuthorizationCore()
                .AddGraphQL()
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("foo")
                        .Authorize(["MyRole",])
                        .Resolve("bar"))
                .AddAuthorization()
                .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void FieldAuth_WithRoles_DescriptorNull()
    {
        // arrange
        // act
        Action action = () =>
            AuthorizeObjectFieldDescriptorExtensions
                .Authorize(null!, ["MyRole",]);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
