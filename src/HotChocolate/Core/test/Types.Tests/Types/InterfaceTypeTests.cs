using System.Reflection;
using System.Text.RegularExpressions;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class InterfaceTypeTests : TypeTestBase
{
    [Fact]
    public void InterfaceType_DynamicName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType(d => d
                .Name(dep => dep.Name + "Foo")
                .DependsOn<StringType>()
                .Field("bar")
                .Type<StringType>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void InterfaceType_DynamicName_NonGeneric()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType(d => d
                .Name(dep => dep.Name + "Foo")
                .DependsOn(typeof(StringType))
                .Field("bar")
                .Type<StringType>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void GenericInterfaceType_DynamicName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType<IFoo>(d => d
                .Name(dep => dep.Name + "Foo")
                .DependsOn<StringType>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void GenericInterfaceType_DynamicName_NonGeneric()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInterfaceType<IFoo>(d => d
                .Name(dep => dep.Name + "Foo")
                .DependsOn(typeof(StringType)))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void InferFieldsFromClrInterface()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType<IFoo>(),
            b => b.ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.Collection(
            fooType.Fields.Where(t => !t.IsIntrospectionField).OrderBy(t => t.Name),
            t =>
            {
                Assert.Equal("bar", t.Name);
                Assert.IsType<BooleanType>(
                    Assert.IsType<NonNullType>(t.Type).Type);
            },
            t =>
            {
                Assert.Equal("baz", t.Name);
                Assert.IsType<StringType>(t.Type);
            },
            t =>
            {
                Assert.Equal("qux", t.Name);
                Assert.IsType<IntType>(
                    Assert.IsType<NonNullType>(t.Type).Type);
                Assert.Collection(t.Arguments,
                    a => Assert.Equal("a", a.Name));
            });
    }

    [Fact]
    public void InferSchemaInterfaceTypeFromClrInterface()
    {
        // arrange && act
        var schema = SchemaBuilder.New()
            .AddType<IFoo>()
            .AddQueryType<FooImpl>()
            .Create();

        // assert
        var type = schema.GetType<ObjectType>("FooImpl");
        Assert.Collection(type.Implements, t => Assert.Equal("IFoo", t.Name));
    }

    [Fact]
    public void IgnoreFieldsFromClrInterface()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType<IFoo>(t => t.Ignore(p => p.Bar)),
            b => b.ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.Collection(
            fooType.Fields.Where(t => !t.IsIntrospectionField),
            t =>
            {
                Assert.Equal("baz", t.Name);
                Assert.IsType<StringType>(t.Type);
            },
            t =>
            {
                Assert.Equal("qux", t.Name);
                Assert.IsType<IntType>(
                    Assert.IsType<NonNullType>(t.Type).Type);
                Assert.Collection(t.Arguments,
                    a => Assert.Equal("a", a.Name));
            });
    }

    [Fact]
    public void UnIgnoreFieldsFromClrInterface()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType<IFoo>(t =>
            {
                t.Ignore(p => p.Bar);
                t.Field(p => p.Bar).Ignore(false);
            }),
            b => b.ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.Collection(
            fooType.Fields.Where(t => !t.IsIntrospectionField),
            t =>
            {
                Assert.Equal("bar", t.Name);
                Assert.IsType<BooleanType>(
                    Assert.IsType<NonNullType>(t.Type).Type);
            },
            t =>
            {
                Assert.Equal("baz", t.Name);
                Assert.IsType<StringType>(t.Type);
            },
            t =>
            {
                Assert.Equal("qux", t.Name);
                Assert.IsType<IntType>(
                    Assert.IsType<NonNullType>(t.Type).Type);
                Assert.Collection(t.Arguments,
                    a => Assert.Equal("a", a.Name));
            });
    }

    [Fact]
    public void ExplicitInterfaceFieldDeclaration()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType<IFoo>(t => t
                .BindFields(BindingBehavior.Explicit)
                .Field(p => p.Bar)),
            b => b.ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.Collection(
            fooType.Fields.Where(t => !t.IsIntrospectionField),
            t =>
            {
                Assert.Equal("bar", t.Name);
                Assert.IsType<BooleanType>(
                    Assert.IsType<NonNullType>(t.Type).Type);
            });
    }

    [Fact]
    public void GenericInterfaceType_AddDirectives_NameArgs()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType<IFoo>(d => d
                .Directive("foo")
                .Field(f => f.Bar)
                .Directive("foo")),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
    }

    [Fact]
    public void GenericInterfaceType_AddDirectives_NameArgs2()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType<IFoo>(d => d
                .Directive("foo")
                .Field(f => f.Bar)
                .Directive("foo")),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
    }

    [Fact]
    public void GenericInterfaceType_AddDirectives_DirectiveNode()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType<IFoo>(d => d
                .Directive(new DirectiveNode("foo"))
                .Field(f => f.Bar)
                .Directive(new DirectiveNode("foo"))),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
    }

    [Fact]
    public void GenericInterfaceType_AddDirectives_DirectiveClassInstance()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType<IFoo>(d => d
                .Directive(new FooDirective())
                .Field(f => f.Bar)
                .Directive(new FooDirective())),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
    }

    [Fact]
    public void GenericInterfaceType_AddDirectives_DirectiveType()
    {
        // arrange
        // act
        var fooType = CreateType(new InterfaceType<IFoo>(d => d
                .Directive<FooDirective>()
                .Field(f => f.Bar)
                .Directive<FooDirective>()),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
    }

    [Fact]
    public void InterfaceType_AddDirectives_NameArgs()
    {
        // arrange
        // act
        var fooType = CreateType(new InterfaceType(d => d
                .Name("FooInt")
                .Directive("foo")
                .Field("id")
                .Type<StringType>()
                .Directive("foo")),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void InterfaceType_AddDirectives_NameArgs2()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType(d => d
                .Name("FooInt")
                .Directive("foo")
                .Field("bar")
                .Type<StringType>()
                .Directive("foo")),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
    }

    [Fact]
    public void InterfaceType_AddDirectives_DirectiveNode()
    {
        // arrange
        // act
        var fooType = CreateType(
            new InterfaceType(d => d
                .Name("FooInt")
                .Directive(new DirectiveNode("foo"))
                .Field("id")
                .Type<StringType>()
                .Directive(new DirectiveNode("foo"))),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void InterfaceType_AddDirectives_DirectiveClassInstance()
    {
        // arrange
        // act
        var fooType = CreateType(new InterfaceType(d => d
                .Name("FooInt")
                .Directive(new FooDirective())
                .Field("id")
                .Type<StringType>()
                .Directive(new FooDirective())),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void InterfaceType_AddDirectives_DirectiveType()
    {
        // arrange
        // act
        var fooType = CreateType(new InterfaceType(d => d
                .Name("FooInt")
                .Directive<FooDirective>()
                .Field("id")
                .Type<StringType>()
                .Directive<FooDirective>()),
            b => b.AddDirectiveType<FooDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false));

        // assert
        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void DoNotAllow_InputTypes_OnFields()
    {
        // arrange
        // act
        Action a = () => SchemaBuilder.New()
            .AddType(new InterfaceType(t => t
                .Name("Foo")
                .Field("bar")
                .Type<NonNullType<InputObjectType<object>>>()))
            .Create();

        // assert
        Assert.Throws<SchemaException>(a)
            .Errors.First().Message.MatchSnapshot();
    }

    [Fact]
    public void DoNotAllow_DynamicInputTypes_OnFields()
    {
        // arrange
        // act
        Action a = () => SchemaBuilder.New()
            .AddType(new InterfaceType(t => t
                .Name("Foo")
                .Field("bar")
                .Type(new NonNullType(new InputObjectType<object>()))))
            .Create();

        // assert
        Assert.Throws<SchemaException>(a)
            .Errors.First().Message.MatchSnapshot();
    }

    [Fact]
    public void Ignore_DescriptorIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() => InterfaceTypeDescriptorExtensions.Ignore<IFoo>(null, t => t.Bar);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Ignore_ExpressionIsNull_ArgumentNullException()
    {
        // arrange
        var descriptor = InterfaceTypeDescriptor.New<IFoo>(DescriptorContext.Create());

        // act
        void Action() => descriptor.Ignore(null);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Ignore_Bar_Property()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(new InterfaceType<IFoo>(d => d
                .Ignore(t => t.Bar)))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Deprecate_Obsolete_Fields()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(new InterfaceType<FooObsolete>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Deprecate_Fields_With_Deprecated_Attribute()
    {
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c.Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(new InterfaceType<FooDeprecated>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void AnnotationBased_Interface_Issue_3577()
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType<Orange>()
            .AddType<Pineapple>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void AnnotationBased_Interface_Issue_3577_Inheritance_Control()
    {
        SchemaBuilder.New()
            .AddQueryType<PetQuery>()
            .AddType<Canine>()
            .AddType<Dog>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void InterfaceType_InInterfaceType_ThrowsSchemaException()
    {
        // arrange
        // act
        var ex = Record.Exception(
            () => SchemaBuilder
                .New()
                .AddQueryType(x => x.Name("Query").Field("Foo").Resolve("bar"))
                .AddType<InterfaceType<InterfaceType<IFoo>>>()
                .ModifyOptions(o => o.StrictRuntimeTypeValidation = true)
                .Create());

        // assert
        Assert.IsType<SchemaException>(ex);
        ex.Message.MatchSnapshot();
    }

    [Fact]
    public void Specify_Field_Type_With_SDL_Syntax()
    {
        SchemaBuilder.New()
            .AddInterfaceType(d =>
            {
                d.Name("Bar");
                d.Field("Foo").Type("String");
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void Specify_Argument_Type_With_SDL_Syntax()
    {
        SchemaBuilder.New()
            .AddInterfaceType(d =>
            {
                d.Name("Bar");
                d.Field("Foo")
                    .Argument("a", t => t.Type("Int"))
                    .Type("String");
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_DeprecatedArguments_Valid()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve(1))
            .AddInterfaceType<DeprecatedInterface>()
            .AddType<DeprecatedImplementation>()
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_DeprecatedArgumentsWithNonNullType_Invalid()
    {
        // arrange

        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve(1))
            .AddInterfaceType<DeprecatedNonNullInterface>()
            .AddType<DeprecatedNonNullImplementation>()
            .BuildRequestExecutorAsync();

        // assert
        var ex = await Assert.ThrowsAsync<SchemaException>(call);
        ex.Errors[0].ToString().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_DeprecatedArguments_Valid()
    {
        // arrange

        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve(1))
            .AddInterfaceType(x => x
                .Name("Interface")
                .Field("bar")
                .Type<IntType>()
                .Argument("baz", y => y.Type<IntType>().Deprecated("b")))
            .AddObjectType(x => x
                .Name("Foo")
                .Implements("Interface")
                .Field("bar")
                .Resolve("asd")
                .Type<IntType>()
                .Argument("baz", y => y.Type<IntType>().Deprecated("b")))
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_DeprecatedArgumentsWithNonNullType_Invalid()
    {
        // arrange

        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve(1))
            .AddInterfaceType(x => x
                .Name("Interface")
                .Field("bar")
                .Type<IntType>()
                .Argument("baz", y => y.Type<NonNullType<IntType>>().Deprecated("b")))
            .AddObjectType(x => x
                .Name("Foo")
                .Implements("Interface")
                .Field("bar")
                .Resolve("asd")
                .Type<IntType>()
                .Argument("baz", y => y.Type<NonNullType<IntType>>().Deprecated("b")))
            .BuildRequestExecutorAsync();

        // assert
        var ex = await Assert.ThrowsAsync<SchemaException>(call);
        ex.Errors[0].ToString().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DeprecatedArguments_Valid()
    {
        // arrange

        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve(1))
            .AddDocumentFromString(@"
                    interface Interface  {
                        bar(a: String @deprecated(reason:""reason"")): Int!
                    }

                    type Foo implements Interface  {
                        bar(a: String @deprecated(reason:""reason"")): Int!
                    }
                ")
            .AddResolver("Foo", "bar", x => 1)
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DeprecatedArgumentsWithNonNullType_Invalid()
    {
        // arrange

        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve(1))
            .AddDocumentFromString(@"
                    interface Interface  {
                        bar(a: String! @deprecated(reason:""reason"")): Int!
                    }

                    type Foo implements Interface  {
                        bar(a: String! @deprecated(reason:""reason"")): Int!
                    }
                ")
            .AddResolver("Foo", "bar", x => 1)
            .BuildRequestExecutorAsync();

        // assert
        var ex = await Assert.ThrowsAsync<SchemaException>(call);
        ex.Errors[0].ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_Interface_Field_Is_Requested_When_Applying_NamingConvention()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddConvention<INamingConventions, SnakeCaseNamingConventions>()
            .AddQueryType(x => x
                .Name("Query")
                .Field("foo")
                .Type<InterfaceType<IFooNaming>>()
                .Resolve(() => null!))
            .AddResolver("Foo", "bar", x => 1)
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task StripLeadingIFromInterface()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("foo")
                .Type<InterfaceType<IFooNaming>>()
                .Resolve(() => null!))
            .AddResolver("Foo", "bar", x => 1)
            .ModifyOptions(o => o.StrictValidation = false)
            .ModifyOptions(o => o.StripLeadingIFromInterface = true)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    private sealed class SnakeCaseNamingConventions : DefaultNamingConventions
    {
        public override string GetMemberName(MemberInfo member, MemberKind kind)
        {
            if (kind == MemberKind.InterfaceField)
            {
                var pattern = new Regex(
                    @"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");
                return string.Join("_", pattern.Matches(member.Name)).ToLower();
            }

            return base.GetMemberName(member, kind);
        }
    }

    private interface IFooNaming
    {
        string FooBarBaz { get; }
    }

    public interface IFoo
    {
        bool Bar { get; }
        string Baz();
        int Qux(string a);
    }

    public class FooImpl : IFoo
    {
        public bool Bar => throw new NotImplementedException();

        public string Baz() => throw new NotImplementedException();

        public int Qux(string a) => throw new NotImplementedException();
    }

    public class FooDirectiveType
        : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor
                .Name("foo")
                .Location(DirectiveLocation.Interface)
                .Location(DirectiveLocation.FieldDefinition);
        }
    }

    public class FooDirective
    {
    }

    public class FooObsolete
    {
        [Obsolete("Baz")]
        public string Bar() => "foo";
    }

    public class FooDeprecated
    {
        [GraphQLDeprecated("Use Bar2.")]
        public string Bar() => "foo";

        public string Bar2() => "Foo 2: Electric foo-galoo";
    }

    public class Query
    {
        public string Hello => "World!";

        public IEnumerable<Fruit> GetFruits() => new Fruit[] { new Orange(), new Pineapple(), };
    }

    [InterfaceType]
    public class DeprecatedInterface
    {
        public int? Deprecated([GraphQLDeprecated("reason")] int? deprecated) => deprecated;
    }

    [InterfaceType]
    public class DeprecatedImplementation : DeprecatedInterface
    {
    }

    [InterfaceType]
    public class DeprecatedNonNullInterface
    {
        public int Deprecated([GraphQLDeprecated("reason")] int deprecated) => deprecated;
    }

    [InterfaceType]
    public class DeprecatedNonNullImplementation : DeprecatedInterface
    {
    }

    [InterfaceType]
    public class Fruit
    {
        public string Taste => "Sweet";
    }

    public class Orange : Fruit
    {
        public string Color => "Orange";
    }

    public class Pineapple : Fruit
    {
        public string Shape => "Strange";
    }

    public class PetQuery
    {
        public Pet GetDog() => new Dog { Name = "Foo", };
    }

    [InterfaceType(Inherited = true)]
    public class Pet
    {
        public string Name { get; set; }
    }

    public class Canine : Pet
    {
    }

    [ObjectType]
    public class Dog : Canine
    {
    }
}
