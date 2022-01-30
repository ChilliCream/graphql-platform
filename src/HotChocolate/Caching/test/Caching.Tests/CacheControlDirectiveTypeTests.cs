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
    public void AnnotateCacheControlToObjectFieldAnnotationMaxAge()
    {
        ISchema schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["justMaxAge"].Directives
            .Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(100, obj.MaxAge);
        Assert.Null(obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldAnnotationScope()
    {
        ISchema schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["justScope"].Directives
            .Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.MaxAge);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldAnnotationInheritMaxAge()
    {
        ISchema schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["justInheritMaxAge"].Directives
            .Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(true, obj.InheritMaxAge);
        Assert.Null(obj.MaxAge);
        Assert.Null(obj.Scope);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldSchemaFirstMaxAge()
    {
        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Query {
                        field: String
                            @cacheControl(maxAge: 600)
                    }")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["field"].Directives
            .Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(600, obj.MaxAge);
        Assert.Null(obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldSchemaFirstScope()
    {
        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Query {
                        field: String
                            @cacheControl(scope: PRIVATE)
                    }")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["field"].Directives
            .Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.MaxAge);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void AnnotateCacheControlToObjectFieldSchemaFirstInheritMaxAge()
    {
        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Query {
                        field: String
                            @cacheControl(inheritMaxAge: true)
                    }")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        ObjectType query = schema.GetType<ObjectType>("Query");
        IDirective directive = query.Fields["field"].Directives
            .Single(t => t.Name == "cacheControl");
        CacheControlDirective obj = directive.ToObject<CacheControlDirective>();

        Assert.Equal(true, obj.InheritMaxAge);
        Assert.Null(obj.MaxAge);
        Assert.Null(obj.Scope);
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
            schema.DirectiveTypes.OfType<CacheControlDirectiveType>().FirstOrDefault()!;

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

    public class Query
    {
        [CacheControl(100)]
        public string JustMaxAge() => "Test";

        [CacheControl(Scope = CacheControlScope.Private)]
        public string JustScope() => "Test";

        [CacheControl(InheritMaxAge = true)]
        public string JustInheritMaxAge() => "Test";
    }
}