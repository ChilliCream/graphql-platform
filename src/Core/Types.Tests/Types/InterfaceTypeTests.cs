using System.Linq;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InterfaceTypeTests
        : TypeTestBase
    {
        [Fact]
        public void InferFieldsFromClrInterface()
        {
            // arrange
            // act
            InterfaceType<IFoo> fooType = CreateType(new InterfaceType<IFoo>());

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
        public void InferSchemaInterfaceTypeFromClrInterface()
        {
            // arrange && act
            var schema = Schema.Create(c =>
            {
                c.RegisterType<IFoo>();
                c.RegisterQueryType<FooImpl>();
            });

            // assert
            ObjectType type = schema.GetType<ObjectType>("FooImpl");
            Assert.Collection(type.Interfaces.Values,
                t => Assert.Equal("IFoo", t.Name));
        }

        [Fact]
        public void IgnoreFieldsFromClrInterface()
        {
            // arrange
            // act
            InterfaceType<IFoo> fooType = CreateType(
                new InterfaceType<IFoo>(t => t.Ignore(p => p.Bar)));

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
        public void ExplicitInterfaceFieldDeclaration()
        {
            // arrange
            // act
            InterfaceType<IFoo> fooType = CreateType(new InterfaceType<IFoo>(t => t
                .BindFields(BindingBehavior.Explicit)
                .Field(p => p.Bar)));

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
            InterfaceType<IFoo> fooType = CreateType(new InterfaceType<IFoo>(d => d
                .Directive("foo")
                .Field(f => f.Bar)
                .Directive("foo")),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            InterfaceType<IFoo> fooType = CreateType(new InterfaceType<IFoo>(d => d
                .Directive(new NameString("foo"))
                .Field(f => f.Bar)
                .Directive(new NameString("foo"))),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            InterfaceType<IFoo> fooType = CreateType(new InterfaceType<IFoo>(d => d
                .Directive(new DirectiveNode("foo"))
                .Field(f => f.Bar)
                .Directive(new DirectiveNode("foo"))),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            InterfaceType<IFoo> fooType = CreateType(new InterfaceType<IFoo>(d => d
                .Directive(new FooDirective())
                .Field(f => f.Bar)
                .Directive(new FooDirective())),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void GenericInterfaceType_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            InterfaceType<IFoo> fooType = CreateType(new InterfaceType<IFoo>(d => d
                .Directive<FooDirective>()
                .Field(f => f.Bar)
                .Directive<FooDirective>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_NameArgs()
        {
            // arrange
            // act
            InterfaceType fooType = CreateType(new InterfaceType(d => d
                .Name("FooInt")
                .Directive("foo")
                .Field("id")
                .Type<StringType>()
                .Directive("foo")),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            InterfaceType fooType = CreateType(new InterfaceType(d => d
                .Name("FooInt")
                .Directive(new NameString("foo"))
                .Field("bar")
                .Type<StringType>()
                .Directive(new NameString("foo"))),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["bar"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            InterfaceType fooType = CreateType(new InterfaceType(d => d
                .Name("FooInt")
                .Directive(new DirectiveNode("foo"))
                .Field("id")
                .Type<StringType>()
                .Directive(new DirectiveNode("foo"))),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            InterfaceType fooType = CreateType(new InterfaceType(d => d
                .Name("FooInt")
                .Directive(new FooDirective())
                .Field("id")
                .Type<StringType>()
                .Directive(new FooDirective())),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InterfaceType_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            InterfaceType fooType = CreateType(new InterfaceType(d => d
                .Name("FooInt")
                .Directive<FooDirective>()
                .Field("id")
                .Type<StringType>()
                .Directive<FooDirective>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        public interface IFoo
        {
            bool Bar { get; }
            string Baz();
            int Qux(string a);
        }

        public class FooImpl
            : IFoo
        {
            public bool Bar => throw new System.NotImplementedException();

            public string Baz()
            {
                throw new System.NotImplementedException();
            }

            public int Qux(string a)
            {
                throw new System.NotImplementedException();
            }
        }

        public class FooDirectiveType
            : DirectiveType<FooDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<FooDirective> descriptor)
            {
                descriptor.Name("foo");
                descriptor.Location(DirectiveLocation.Interface)
                    .Location(DirectiveLocation.FieldDefinition);
            }
        }

        public class FooDirective { }
    }
}
