using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class UnionTypeTests
        : TypeTestBase
    {
        [Fact]
        public void UnionType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new UnionType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()
                    .Type<FooType>()
                    .Type<BarType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            UnionType type = schema.GetType<UnionType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void UnionType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new UnionType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))
                    .Type<FooType>()
                    .Type<BarType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            UnionType type = schema.GetType<UnionType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericUnionType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new UnionType<IFooOrBar>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()));

                c.RegisterType(new FooType());
                c.RegisterType(new BarType());

                c.Options.StrictValidation = false;
            });

            // assert
            UnionType type = schema.GetType<UnionType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericUnionType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new UnionType<IFooOrBar>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))));

                c.RegisterType(new FooType());
                c.RegisterType(new BarType());

                c.Options.StrictValidation = false;
            });

            // assert
            UnionType type = schema.GetType<UnionType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void DeclareUnion_ByProvidingExplicitTypeSet()
        {
            // arrange
            // act
            UnionType fooBarType = CreateType(new UnionType(d => d
                .Name("FooOrBar")
                .Type<FooType>()
                .Type<BarType>()));

            // assert
            Assert.Collection(fooBarType.Types.Values,
                t => Assert.Equal("Foo", t.Name),
                t => Assert.Equal("Bar", t.Name));
        }

        [Fact]
        public void DeclareUnion_InferTypeSetFromMarkerInterface()
        {
            // arrange
            // act
            UnionType<IFooOrBar> fooBarType = CreateType(
                new UnionType<IFooOrBar>(),
                b => b.AddTypes(new FooType(), new BarType()));

            // assert
            Assert.Collection(fooBarType.Types.Values,
                t => Assert.Equal("Foo", t.Name),
                t => Assert.Equal("Bar", t.Name));
        }

        [Fact]
        public void DeclareUnion_MarkerInterfaceAndTypeSet()
        {
            // arrange
            // act
            UnionType<IFooOrBar> fooBarType = CreateType(
                new UnionType<IFooOrBar>(c => c.Type<BazType>()),
                b => b.AddTypes(new FooType(), new BarType()));

            // assert
            Assert.Collection(fooBarType.Types.Values,
                t => Assert.Equal("Baz", t.Name),
                t => Assert.Equal("Foo", t.Name),
                t => Assert.Equal("Bar", t.Name));
        }

        [Fact]
        public void UnionType_AddDirectives_NameArgs()
        {
            // arrange
            // act
            UnionType fooBarType = CreateType(new UnionType(d => d
                .Name("BarUnion")
                .Directive("foo")
                .Type<FooType>()
                .Type<BarType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarType.Directives["foo"]);
        }

        [Fact]
        public void UnionType_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            UnionType fooBarType = CreateType(new UnionType(d => d
                .Name("BarUnion")
                .Directive(new NameString("foo"))
                .Type<FooType>()
                .Type<BarType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarType.Directives["foo"]);
        }

        [Fact]
        public void UnionType_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            UnionType fooBarType = CreateType(new UnionType(d => d
                .Name("BarUnion")
                .Directive(new DirectiveNode("foo"))
                .Type<FooType>()
                .Type<BarType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarType.Directives["foo"]);
        }

        [Fact]
        public void UnionType_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            UnionType fooBarType = CreateType(new UnionType(d => d
                .Name("BarUnion")
                .Directive(new FooDirective())
                .Type<FooType>()
                .Type<BarType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarType.Directives["foo"]);
        }

        [Fact]
        public void UnionType_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            UnionType fooBarType = CreateType(new UnionType(d => d
                .Name("BarUnion")
                .Directive<FooDirective>()
                .Type<FooType>()
                .Type<BarType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarType.Directives["foo"]);
        }

        public class FooType
            : ObjectType<Foo>
        {
        }

        public class BarType
            : ObjectType<Bar>
        {
        }

        public class BazType
            : ObjectType<Baz>
        {
        }

        public class Foo
            : IFooOrBar
        {
            public string FooField { get; set; }
        }

        public class Bar
            : IFooOrBar
        {
            public string BarField { get; set; }
        }

        public class Baz
            : IFooOrBar
        {
            public string BazField { get; set; }
        }

        public interface IFooOrBar
        {
        }

        public class FooDirectiveType
            : DirectiveType<FooDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<FooDirective> descriptor)
            {
                descriptor.Name("foo");
                descriptor.Location(DirectiveLocation.Union)
                    .Location(DirectiveLocation.FieldDefinition);
            }
        }

        public class FooDirective { }
    }
}
