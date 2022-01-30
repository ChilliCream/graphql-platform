using System.Linq;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlDirectiveTypeTests
{
    [Fact]
    public void AnnotateCacheControlToObjectFieldCodeFirstMaxAge()
    {
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .CacheControl(1000))
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["field"].Directives.Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(1000, obj.MaxAge);
        Assert.Null(obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldCodeFirstScope()
    {
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .CacheControl(scope: CacheControlScope.Private))
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["field"].Directives.Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.MaxAge);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldCodeFirstInheritMaxAge()
    {
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .CacheControl(inheritMaxAge: true))
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["field"].Directives.Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(true, obj.InheritMaxAge);
        Assert.Null(obj.MaxAge);
        Assert.Null(obj.Scope);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldCodeFirstAllArguments()
    {
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .CacheControl(500, CacheControlScope.Public, false))
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["field"].Directives.Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Public, obj.Scope);
        Assert.Equal(false, obj.InheritMaxAge);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldSchemaFirst()
    {
        // arrange
        // act
        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Query {
                        field: String
                            @cacheControl(maxAge: 600 scope: PRIVATE inheritMaxAge: true)
                    }")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["field"].Directives
            .Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();
        Assert.Equal(600, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Equal(true, obj.InheritMaxAge);
    }

    [Fact]
    public void CreateCacheControlDirective()
    {
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Type<StringType>())
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();
        CacheControlDirectiveType directive =
            schema.DirectiveTypes.OfType<CacheControlDirectiveType>().FirstOrDefault();

        Assert.NotNull(directive);
        Assert.IsType<CacheControlDirectiveType>(directive);
        Assert.Equal("cacheControl", directive.Name);
        Assert.Collection(directive.Arguments,
            t =>
            {
                Assert.Equal("maxAge", t.Name);
                Assert.IsType<IntType>(t.Type);
            },
            t =>
            {
                Assert.Equal("scope", t.Name);
                Assert.IsType<CacheControlScopeType>(t.Type);
            },
            t =>
            {
                Assert.Equal("inheritMaxAge", t.Name);
                Assert.IsType<BooleanType>(t.Type);
            });
        Assert.Collection(directive.Locations,
            t => Assert.Equal(DirectiveLocation.FieldDefinition, t));
    }
}