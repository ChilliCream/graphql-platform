using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using SnapshotExtensions = CookieCrumble.SnapshotExtensions;
using static HotChocolate.Types.FieldBindingFlags;

#nullable enable

namespace HotChocolate.Types;

public class ObjectTypeExtensionTests
{
    [Fact]
    public async Task ObjectTypeExtension_AddField()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension<FooTypeExtension>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields.ContainsField("test"));
    }

    [Fact]
    public async Task ObjectTypeExtension_Infer_Field()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension<GenericFooTypeExtension>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields.ContainsField("test"));
    }

    [Fact]
    public async Task ObjectTypeExtension_Declare_Field()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension<FooExtension>(d =>
            {
                d.Name("Foo");
                d.Field(t => t.Test).Type<IntType>();
            }))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields.ContainsField("test"));
        Assert.IsType<IntType>(type.Fields["test"].Type);
    }

    [Fact]
    public async Task ObjectTypeExtension_Remove_Field_By_Name()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Ignore(true)))
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ObjectTypeExtension_Remove_Field()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension<Foo>(d => d
                .Ignore(f => f.Description)))
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ObjectTypeExtension_Execute_Infer_Field()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension<GenericFooTypeExtension>()
            .ExecuteRequestAsync("{ test }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ObjectTypeExtension_OverrideResolver()
    {
        ValueTask<object?> Resolver(IResolverContext ctx) => new(null!);

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Type<StringType>()
                .Resolve(Resolver)))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.Equal(Resolver, type.Fields["description"].Resolver);
    }

    [Fact]
    public async Task ObjectTypeExtension_AddResolverType()
    {
        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        context.Setup(t => t.Resolver<FooResolver>())
            .Returns(new FooResolver());
        context.Setup(t => t.RequestAborted)
            .Returns(CancellationToken.None);

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field<FooResolver>(t => t.GetName2())
                .Type<StringType>()))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        var value = await type.Fields["name2"].Resolver!.Invoke(context.Object);
        Assert.Equal("FooResolver.GetName2", value);
    }

    [Fact]
    public async Task ObjectTypeExtension_AddMiddleware()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Type<StringType>()
                .Use(_ => context =>
                {
                    context.Result = "BAR";
                    return default;
                })))
            .ExecuteRequestAsync("{ description }")
            .ToJsonAsync()
            .MatchSnapshotAsync();
    }

    [Obsolete]
    [Fact]
    public async Task ObjectTypeExtension_DeprecateField_Obsolete()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Type<StringType>()
                .DeprecationReason("Foo")))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields["description"].IsDeprecated);
        Assert.Equal("Foo", type.Fields["description"].DeprecationReason);
    }

    [Fact]
    public async Task ObjectTypeExtension_DeprecateField_With_Reason()
    {
        Snapshot.FullName();

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Type<StringType>()
                .Deprecated("Foo")))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields["description"].IsDeprecated);
        Assert.Equal("Foo", type.Fields["description"].DeprecationReason);
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task ObjectTypeExtension_DeprecateField_Without_Reason()
    {
        Snapshot.FullName();

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Type<StringType>()
                .Deprecated()))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields["description"].IsDeprecated);
        Assert.Equal(
            WellKnownDirectives.DeprecationDefaultReason,
            type.Fields["description"].DeprecationReason);
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task ObjectTypeExtension_SetTypeContextData()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Extend()
                .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.ContextData.ContainsKey("foo"));
    }

    [Fact]
    public async Task ObjectTypeExtension_SetFieldContextData()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Extend()
                .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields["description"]
            .ContextData.ContainsKey("foo"));
    }

    [Fact]
    public async Task ObjectTypeExtension_SetArgumentContextData()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Type<StringType>()
                .Argument("a", a => a
                    .Type<StringType>()
                    .Extend()
                    .OnBeforeCreate(c => c.ContextData["foo"] = "bar"))))
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields["name"].Arguments["a"]
            .ContextData.ContainsKey("foo"));
    }

    [Fact]
    public async Task ObjectTypeExtension_SetDirectiveOnType()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy")))
            .AddDirectiveType<DummyDirective>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public async Task ObjectTypeExtension_SetDirectiveOnField()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Directive("dummy")))
            .AddDirectiveType<DummyDirective>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields["name"].Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public async Task ObjectTypeExtension_SetDirectiveOnArgument()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Argument("a", a => a.Directive("dummy"))))
            .AddDirectiveType<DummyDirective>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields["name"].Arguments["a"].Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public async Task ObjectTypeExtension_CopyDependencies_ToType()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Argument("a", a => a.Directive("dummy_arg", new ArgumentNode("a", "b")))))
            .AddDirectiveType<DummyWithArgDirective>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        var value = type.Fields["name"].Arguments["a"]
            .Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public async Task ObjectTypeExtension_RepeatableDirectiveOnType()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(new ObjectType<Foo>(t => t
                .Directive("dummy_rep")))
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy_rep")))
            .AddDirectiveType<RepeatableDummyDirective>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        var count = type.Directives["dummy_rep"].Count();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ObjectTypeExtension_RepeatableDirectiveOnField()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Directive("dummy_rep")))
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Directive("dummy_rep")))
            .AddDirectiveType<RepeatableDummyDirective>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        var count = type.Fields["description"].Directives["dummy_rep"].Count();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ObjectTypeExtension_RepeatableDirectiveOnArgument()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(new ObjectType<Foo>(t => t
                .Field(f => f.GetName(default!))
                .Argument("a", a => a
                    .Type<StringType>()
                    .Directive("dummy_rep", new ArgumentNode("a", "a")))))
            .AddTypeExtension(new ObjectTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Argument("a", a =>
                    a.Directive("dummy_rep", new ArgumentNode("a", "b")))))
            .AddDirectiveType<RepeatableDummyDirective>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        var count = type.Fields["name"].Arguments["a"]
            .Directives["dummy_rep"]
            .Count();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ObjectTypeExtension_SetDirectiveOnArgument_Sdl_First()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooType>()
            .AddDocumentFromString(
                @"extend type Foo {
                        name(a: String @dummy): String
                    }")
            .AddDirectiveType<DummyDirective>()
            .BuildSchemaAsync();

        var type = schema.GetType<ObjectType>("Foo");
        Assert.True(type.Fields["name"].Arguments["a"].Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public async Task BindByType()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<Query>()
            .AddTypeExtension<Extensions>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

#if NET6_0_OR_GREATER
    [Fact]
    public async Task BindByType_With_Generic_Attribute()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<Query>()
            .AddTypeExtension<Extensions2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }
#endif

    [Fact]
    public async Task BindResolver_With_Property()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<BindResolver_With_Property_PersonDto>()
            .AddTypeExtension<BindResolver_With_Property_PersonResolvers>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task BindResolver_With_Field()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<BindResolver_With_Property_PersonDto>()
            .AddTypeExtension<BindResolver_With_Field_PersonResolvers>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Remove_Properties_Globally()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Remove_Properties_Globally_PersonDto>()
            .AddTypeExtension<Remove_Properties_Globally_PersonResolvers>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Remove_Fields_Globally()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Remove_Fields_Globally_PersonDto>()
            .AddTypeExtension<Remove_Fields_Globally_PersonResolvers>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Remove_Fields()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Remove_Fields_PersonDto>()
            .AddTypeExtension<Remove_Fields_PersonResolvers>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Remove_Fields_BindField()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Remove_Fields_BindProperty_PersonDto>()
            .AddTypeExtension<Remove_Fields_BindProperty_PersonResolvers>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Replace_Field()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Replace_Field_PersonDto>()
            .AddTypeExtension<Replace_Field_PersonResolvers>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Replace_Field_With_The_Same_Name()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType()
            .AddTypeExtension<Replace_Field_PersonDto_2_Query>()
            .AddTypeExtension<Replace_Field_PersonResolvers_2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Replace_Field_With_The_Same_Name_Execute()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType()
            .AddTypeExtension<Replace_Field_PersonDto_2_Query>()
            .AddTypeExtension<Replace_Field_PersonResolvers_2>()
            .ExecuteRequestAsync("{ person { someId(arg: \"efg\") } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Extended_Field_Overwrites_Extended_Field()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType()
            .AddTypeExtension<ExtensionA>()
            .AddTypeExtension<ExtensionB>()
            .ExecuteRequestAsync("{ foo }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Member_And_ResolverMember_Are_Correctly_Set_When_Extending()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ObjectField_Test_Query>()
                .AddTypeExtension<ObjectField_Test_Query_Extension>()
                .BuildSchemaAsync();

        IObjectField field = schema.QueryType.Fields["foo1"];
        Assert.Equal("GetFoo", field.Member?.Name);
        Assert.Equal("GetFoo1", field.ResolverMember?.Name);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public async Task Ensure_Member_And_ResolverMember_Are_Correctly_Set_When_Extending_Generic()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ObjectField_Test_Query>()
                .AddTypeExtension<ObjectField_Test_Query_Extension_Generic>()
                .BuildSchemaAsync();

        IObjectField field = schema.QueryType.Fields["foo1"];
        Assert.Equal("GetFoo", field.Member?.Name);
        Assert.Equal("GetFoo1", field.ResolverMember?.Name);
    }
#endif

    [Fact]
    public async Task Ensure_Member_And_ResolverMember_Are_The_Same_When_Not_Extending()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ObjectField_Test_Query>()
                .BuildSchemaAsync();

        IObjectField field = schema.QueryType.Fields["foo"];
        Assert.Equal("GetFoo", field.Member?.Name);
        Assert.Equal("GetFoo", field.ResolverMember?.Name);
    }

    [Fact]
    public async Task Descriptor_Attributes_Are_Applied_On_Resolvers()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FooQueryType>()
            .ExecuteRequestAsync("{ sayHello }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Static_Query_Extensions()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddTypeExtension(typeof(StaticExtensions))
                .ExecuteRequestAsync("{ hello }");

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Static_Query_Extensions_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddTypeExtension(typeof(StaticExtensions))
                .BuildSchemaAsync();

        SnapshotExtensions.MatchSnapshot(schema);
    }

    [Fact]
    public async Task Query_Extension_With_Static_Members_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddTypeExtension<QueryExtensionWithStaticField>()
                .BuildSchemaAsync();

        SnapshotExtensions.MatchSnapshot(schema);
    }


    [Fact]
    public async Task Query_Extension_With_Static_Members_2_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddTypeExtension<QueryExtensionWithStaticField2>()
                .ModifyOptions(t => t.DefaultFieldBindingFlags = InstanceAndStatic)
                .BuildSchemaAsync();

        SnapshotExtensions.MatchSnapshot(schema);
    }

    [Fact]
    public async Task Query_Extension_With_Static_Members_Execute()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddTypeExtension<QueryExtensionWithStaticField>()
                .ExecuteRequestAsync("{ hello }");

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Query_Extension_With_Static_Members_And_Generic_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<FooQuery>()
                .AddTypeExtension(typeof(StaticFooQueryExtensions))
                .BuildSchemaAsync();

        SnapshotExtensions.MatchSnapshot(schema);
    }

    [Fact]
    public async Task Query_Extension_With_Static_Members_And_Generic()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<FooQuery>()
                .AddTypeExtension(typeof(StaticFooQueryExtensions))
                .ExecuteRequestAsync("{ hello }");

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task ExtendObjectTypeAttribute_Extends_SchemaType()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddTypeExtension<QueryExtensions>()
                .BuildSchemaAsync();

        SnapshotExtensions.MatchSnapshot(schema);
    }

    public class FooType : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Description);
        }
    }

    public class FooTypeExtension : ObjectTypeExtension
    {
        protected override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Name("Foo")
                .Field("test")
                .Resolve(() => new List<string>())
                .Type<ListType<StringType>>();
        }
    }

    public class GenericFooTypeExtension : ObjectTypeExtension<FooExtension>
    {
        protected override void Configure(
            IObjectTypeDescriptor<FooExtension> descriptor)
        {
            descriptor.Name("Foo");
        }
    }

    public class Foo
    {
        public string? Description => "hello";

        public string? GetName(string? a) => default!;
    }

    public class FooExtension
    {
        public string Test { get; set; } = "Test123";
    }

    public class FooResolver
    {
        public string GetName2()
        {
            return "FooResolver.GetName2";
        }
    }

    public class DummyDirective : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy");
            descriptor.Location(DirectiveLocation.Object);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }

    public class DummyWithArgDirective : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy_arg");
            descriptor.Argument("a").Type<StringType>();
            descriptor.Location(DirectiveLocation.Object);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }

    public class RepeatableDummyDirective : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy_rep");
            descriptor.Repeatable();
            descriptor.Argument("a").Type<StringType>();
            descriptor.Location(DirectiveLocation.Object);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }

    public class Query : IMarker
    {
        public string? Foo { get; } = "abc";
    }

    public class Bar : IMarker
    {
        public string? Baz { get; } = "def";
    }

    [ExtendObjectType(
        // extends all types that inherit this type.
        extendsType: typeof(IMarker))]
    public class Extensions
    {
        // introduces a new field on all types that apply the parent
        public string? Any([Parent] object parent)
        {
            if (parent is Query q)
            {
                return q.Foo;
            }

            if (parent is Bar b)
            {
                return b.Baz;
            }

            return null;
        }

        // replaces the original field baz on bar
        [GraphQLName("baz")]
        public string? BazEx([Parent] Bar bar)
        {
            return bar.Baz;
        }

        // introduces a new field to query
        public Bar? FooEx([Parent] Query query)
        {
            return new();
        }
    }

#if NET6_0_OR_GREATER
    [ExtendObjectType<IMarker>]
    public class Extensions2
    {
        // introduces a new field on all types that apply the parent
        public string? Any([Parent] object parent)
        {
            if (parent is Query q)
            {
                return q.Foo;
            }

            if (parent is Bar b)
            {
                return b.Baz;
            }

            return null;
        }

        // replaces the original field baz on bar
        [GraphQLName("baz")]
        public string? BazEx([Parent] Bar bar)
        {
            return bar.Baz;
        }

        // introduces a new field to query
        public Bar? FooEx([Parent] Query query)
        {
            return new();
        }
    }
#endif

    public interface IMarker
    {

    }

    public class BindResolver_With_Property_PersonDto
    {
        public int FriendId => 1;
    }

    [ExtendObjectType(typeof(BindResolver_With_Property_PersonDto))]
    public class BindResolver_With_Property_PersonResolvers
    {
        [BindMember(nameof(BindResolver_With_Property_PersonDto.FriendId))]
        public List<BindResolver_With_Property_PersonDto?> Friends() => [];
    }

    [ExtendObjectType(typeof(BindResolver_With_Property_PersonDto))]
    public class BindResolver_With_Field_PersonResolvers
    {
        [BindFieldAttribute("friendId")]
        public List<BindResolver_With_Property_PersonDto?> Friends() => [];
    }

    public class Remove_Properties_Globally_PersonDto
    {
        public int FriendId { get; } = 1;

        public int InternalId { get; } = 1;
    }

    [ExtendObjectType(
        typeof(Remove_Properties_Globally_PersonDto),
        IgnoreProperties = [nameof(Remove_Properties_Globally_PersonDto.InternalId),])]
    public class Remove_Properties_Globally_PersonResolvers
    {
    }

    public class Remove_Fields_Globally_PersonDto
    {
        public int FriendId { get; } = 1;

        public int InternalId { get; } = 1;
    }

    [ExtendObjectType(
        typeof(Remove_Fields_Globally_PersonDto),
        IgnoreProperties = ["internalId",])]
    public class Remove_Fields_Globally_PersonResolvers
    {
    }

    public class Remove_Fields_PersonDto
    {
        public int FriendId { get; } = 1;

        public int InternalId { get; } = 1;
    }

    [ExtendObjectType(typeof(Remove_Fields_PersonDto))]
    public class Remove_Fields_PersonResolvers
    {
        [GraphQLIgnore]
        public int InternalId { get; } = 1;
    }

    public class Remove_Fields_BindProperty_PersonDto
    {
        public int FriendId { get; } = 1;

        public int InternalId { get; } = 1;
    }

    [ExtendObjectType(typeof(Remove_Fields_BindProperty_PersonDto))]
    public class Remove_Fields_BindProperty_PersonResolvers
    {
        [GraphQLIgnore]
        [BindMember(nameof(Remove_Fields_BindProperty_PersonDto.InternalId))]
        public int SomeId { get; } = 1;
    }

    public class Replace_Field_PersonDto
    {
        public int FriendId { get; } = 1;

        public int InternalId { get; } = 1;
    }

    [ExtendObjectType(typeof(Replace_Field_PersonDto))]
    public class Replace_Field_PersonResolvers
    {
        [BindMember(nameof(Replace_Field_PersonDto.InternalId))]
        public string? SomeId { get; } = "abc";
    }

    public interface IPersonDto
    {
        string SomeId();
    }

    [ExtendObjectType("Query")]
    public class Replace_Field_PersonDto_2_Query
    {
        public Replace_Field_PersonDto_2? GetPerson() => new();
    }

    public class Replace_Field_PersonDto_2 : IPersonDto
    {
        public string SomeId() => "1";
    }

    [ExtendObjectType(typeof(IPersonDto))]
    public class Replace_Field_PersonResolvers_2
    {
        [BindMember(nameof(Replace_Field_PersonDto_2.SomeId))]
        public string? SomeId([Parent] IPersonDto dto, string? arg = "abc") =>
            dto.SomeId() + arg;
    }

    [ExtendObjectType(OperationTypeNames.Query)]
    public class ExtensionA
    {
        public string Foo() => "abc";
    }

    [ExtendObjectType(OperationTypeNames.Query)]
    public class ExtensionB
    {
        public string Foo() => "def";
    }

    public class ObjectField_Test_Query
    {
        public string GetFoo() => null!;
    }

    [ExtendObjectType(typeof(ObjectField_Test_Query))]
    public class ObjectField_Test_Query_Extension
    {
        [BindMember(nameof(ObjectField_Test_Query.GetFoo))]
        public string GetFoo1() => null!;
    }

#if NET6_0_OR_GREATER
    [ExtendObjectType<ObjectField_Test_Query>]
    public class ObjectField_Test_Query_Extension_Generic
    {
        [BindMember(nameof(ObjectField_Test_Query.GetFoo))]
        public string GetFoo1() => null!;
    }
#endif

    public class FooQueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field("sayHello").ResolveWith<FooBar>(t => t.SayHello());
        }
    }

    public class FooBar
    {
        [Foo]
        public string SayHello() => "Huhu";
    }

    public class FooAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Use(_ => ctx =>
            {
                ctx.Result = "Hello";
                return default;
            });
        }
    }

    [ExtendObjectType(OperationType.Query, IncludeStaticMembers = true)]
    public class QueryExtensionWithStaticField
    {
        public static string Hello()
            => "abc";
    }

    [ExtendObjectType(OperationType.Query)]
    public class QueryExtensionWithStaticField2
    {
        public static string Hello()
            => "abc";
    }

    [ExtendObjectType(OperationType.Query)]
    public static class StaticExtensions
    {
        public static string Hello()
            => "abc";
    }

    public class FooQuery
    {
        public string Abc { get; } = "def";
    }

    [ExtendObjectType<FooQuery>]
    public static class StaticFooQueryExtensions
    {
        public static string Hello([Parent] FooQuery query)
            => query.Abc;
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field("foo").Resolve("bar");
        }
    }

    [ExtendObjectType<QueryType>]
    public class QueryExtensions
    {
        public string Bar() => "baz";
    }
}
