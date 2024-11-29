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
        Assert.Empty(authorizeDirective.Roles!);
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
            authorizeDirective.Roles!,
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t));
    }

    [Fact]
    public void CacheKey_Policy_NoRoles()
    {
        // arrange
        var authorizeDirective = new AuthorizeDirective(
            policy: "policy");

        // act
        var cacheKey = authorizeDirective.GetPolicyCacheKey();

        // assert
        Assert.Equal("policy;", cacheKey);
    }

    [Fact]
    public void CacheKey_NoPolicy_Roles()
    {
        // arrange
        var authorizeDirective = new AuthorizeDirective(
            policy: null,
            roles: ["a", "b"]);

        // act
        var cacheKey = authorizeDirective.GetPolicyCacheKey();

        // assert
        Assert.Equal(";a,b", cacheKey);
    }

    [Fact]
    public void CacheKey_Policy_And_Roles()
    {
        // arrange
        var authorizeDirective = new AuthorizeDirective(
            policy: "policy",
            roles: ["a", "b"]);

        // act
        var cacheKey = authorizeDirective.GetPolicyCacheKey();

        // assert
        Assert.Equal("policy;a,b", cacheKey);
    }

    [Fact]
    public void CacheKey_NoPolicy_NoRoles()
    {
        // arrange
        var authorizeDirective = new AuthorizeDirective(
            policy: null,
            roles: null);

        // act
        var cacheKey = authorizeDirective.GetPolicyCacheKey();

        // assert
        Assert.Equal("", cacheKey);
    }

    [Fact]
    public void CacheKey_Policy_And_Role_Naming_Does_Not_Conflict()
    {
        // arrange
        var authorizeDirective1 = new AuthorizeDirective(
            policy: "policy",
            roles: null);

        var authorizeDirective2 = new AuthorizeDirective(
            policy: null,
            roles: ["policy"]);

        // act
        var cacheKey1 = authorizeDirective1.GetPolicyCacheKey();
        var cacheKey2 = authorizeDirective2.GetPolicyCacheKey();

        // assert
        Assert.NotEqual(cacheKey1, cacheKey2);
    }

    [Fact]
    public void CacheKey_Same_Roles_Albeit_Sorted_Differently_Have_Same_Cache_Key()
    {
        // arrange
        var authorizeDirective1 = new AuthorizeDirective(
            policy: null,
            roles: ["a", "c", "b"]);

        var authorizeDirective2 = new AuthorizeDirective(
            policy: null,
            roles: ["c", "b", "a"]);

        // act
        var cacheKey1 = authorizeDirective1.GetPolicyCacheKey();
        var cacheKey2 = authorizeDirective2.GetPolicyCacheKey();

        // assert
        Assert.Equal(cacheKey1, cacheKey2);
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
