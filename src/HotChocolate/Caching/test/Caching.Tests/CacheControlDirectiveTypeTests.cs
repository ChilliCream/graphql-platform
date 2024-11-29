using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlDirectiveTypeTests
{
    [Fact]
    public void CreateCacheControlDirective()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Type<StringType>())
            .AddDirectiveType<CacheControlDirectiveType>()
            .ModifyOptions(o => o.RemoveUnusedTypeSystemDirectives = false)
            .Use(_ => _)
            .Create();
        var directive =
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
                Assert.Equal("sharedMaxAge", t.Name);
                Assert.IsType<IntType>(t.Type);
            },
            t =>
            {
                Assert.Equal("inheritMaxAge", t.Name);
                Assert.IsType<BooleanType>(t.Type);
            },
            t =>
            {
                Assert.Equal("scope", t.Name);
                Assert.IsType<CacheControlScopeType>(t.Type);
            },
            t =>
            {
                Assert.Equal("vary", t.Name);
                Assert.IsType<ListType>(t.Type);
                Assert.IsType<StringType>(t.Type.ElementType());
            });
        Assert.Collection(
            directive.Locations.AsEnumerable(),
            t => Assert.Equal(Types.DirectiveLocation.Object, t),
            t => Assert.Equal(Types.DirectiveLocation.FieldDefinition, t),
            t => Assert.Equal(Types.DirectiveLocation.Interface, t),
            t => Assert.Equal(Types.DirectiveLocation.Union, t));
    }

    [Fact]
    public void CacheControlDirective_Cannot_Be_Applied_Multiple_Times()
    {
        var builder = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("ObjectType")
                .Field("field")
                .Type<StringType>()
                .CacheControl(500)
                .CacheControl(1000))
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _);

        var act = () => builder.Create();

        var expectedException = Assert.Throws<SchemaException>(act);
        expectedException.Message.MatchSnapshot();
    }

    [Fact]
    public void CacheControlDirectiveType_ObjectField_CodeFirst()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("ObjectType")
                .Field("field")
                .Type<StringType>()
                .CacheControl(500, CacheControlScope.Private, true))
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<ObjectType>("ObjectType");
        var directive = type.Fields["field"].Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Equal(true, obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_ObjectField_SchemaFirst()
    {
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
            @"
            type Query {
                field: ObjectType
            }

            type ObjectType {
                field: String @cacheControl(maxAge: 500 scope: PRIVATE inheritMaxAge: true)
            }
            ")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<ObjectType>("ObjectType");
        var directive = type.Fields["field"].Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Equal(true, obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_ObjectField_Annotation()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType<ObjectQuery>()
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<ObjectType>("ObjectType");
        var directive = type.Fields["field"].Directives
            .Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Equal(true, obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_ObjectType_CodeFirst()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("ObjectType")
                .CacheControl(500, CacheControlScope.Private)
                .Field("field")
                .Type<StringType>())
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<ObjectType>("ObjectType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_ObjectType_SchemaFirst()
    {
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
            @"
            type Query {
                field: ObjectType
            }

            type ObjectType @cacheControl(maxAge: 500 scope: PRIVATE inheritMaxAge: true) {
                field: String
            }
            ")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<ObjectType>("ObjectType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Equal(true, obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_ObjectType_Annotation()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType<ObjectQuery>()
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<ObjectType>("ObjectType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_InterfaceField_SchemaFirst()
    {
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
            @"
            type Query {
                field: InterfaceType
            }

            interface InterfaceType {
                field: String @cacheControl(maxAge: 500 scope: PRIVATE inheritMaxAge: true)
            }

            type ObjectType implements InterfaceType {
                field: String
            }
            ")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<InterfaceType>("InterfaceType");
        var directive = type.Fields["field"].Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Equal(true, obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_InterfaceField_Annotation()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType<InterfaceQuery>()
            .AddType<InterfaceObjectType>()
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<InterfaceType>("InterfaceType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_InterfaceType_CodeFirst()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Type<StringType>())
            .AddInterfaceType(d => d
                .Name("InterfaceType")
                .CacheControl(500, CacheControlScope.Private)
                .Field("field")
                .Type<StringType>())
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<InterfaceType>("InterfaceType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_InterfaceType_SchemaFirst()
    {
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
            @"
            type Query {
                field: InterfaceType
            }

            interface InterfaceType @cacheControl(maxAge: 500 scope: PRIVATE inheritMaxAge: true) {
                field: String
            }

            type ObjectType implements InterfaceType {
                field: String
            }
            ")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<InterfaceType>("InterfaceType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Equal(true, obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_InterfaceType_Annotation()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType<InterfaceQuery>()
            .AddType<InterfaceObjectType>()
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<InterfaceType>("InterfaceType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_UnionType_CodeFirst()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Type<StringType>())
            .AddUnionType(d => d
                .Name("UnionType")
                .CacheControl(500, CacheControlScope.Private)
                .Type(new NamedTypeNode("ObjectType")))
            .AddObjectType(d => d
                .Name("ObjectType")
                .Field("field")
                .Type<StringType>())
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<UnionType>("UnionType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_UnionType_SchemaFirst()
    {
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
            @"
            type Query {
                field: UnionType
            }

            union UnionType @cacheControl(maxAge: 500 scope: PRIVATE inheritMaxAge: true) = ObjectType

            type ObjectType {
                field: String
            }
            ")
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<UnionType>("UnionType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Equal(true, obj.InheritMaxAge);
    }

    [Fact]
    public void CacheControlDirectiveType_UnionType_Annotation()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType<UnionQuery>()
            .AddType<UnionObjectType>()
            .AddDirectiveType<CacheControlDirectiveType>()
            .Use(_ => _)
            .Create();

        var type = schema.GetType<UnionType>("UnionType");
        var directive = type.Directives.Single(d => d.Type.Name == "cacheControl");
        var obj = directive.AsValue<CacheControlDirective>();

        Assert.Equal(500, obj.MaxAge);
        Assert.Equal(CacheControlScope.Private, obj.Scope);
        Assert.Null(obj.InheritMaxAge);
    }

    [ObjectType("ObjectType")]
    [CacheControl(500, Scope = CacheControlScope.Private, InheritMaxAge = true)]
    public class ObjectQuery
    {
        [CacheControl(500, Scope = CacheControlScope.Private, InheritMaxAge = true)]
        public string? Field { get; set; }
    }

    [InterfaceType("InterfaceType")]
    [CacheControl(500, Scope = CacheControlScope.Private, InheritMaxAge = true)]
    public interface IInterfaceType
    {
        [CacheControl(500, Scope = CacheControlScope.Private, InheritMaxAge = true)]
        public string? Field { get; set; }
    }

    public class InterfaceObjectType : IInterfaceType
    {
        public string? Field { get; set; }
    }

    public class InterfaceQuery
    {
        public IInterfaceType? GetField() => null;
    }

    [UnionType("UnionType")]
    [CacheControl(500, Scope = CacheControlScope.Private, InheritMaxAge = true)]
    public interface IUnionType
    {
    }

    public class UnionObjectType : IUnionType
    {
        public string? Field { get; set; }
    }

    public class UnionQuery
    {
        public IUnionType? GetField() => null;
    }
}
