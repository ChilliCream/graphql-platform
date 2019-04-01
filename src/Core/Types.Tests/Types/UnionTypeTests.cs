using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class UnionTypeTests
        : TypeTestBase
    {
        [Fact]
        public void DeclareUnion_ByProvidingExplicitTypeSet()
        {
            // arrange
            // act
            UnionType fooBarType = CreateType(new UnionType(d => d
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
            UnionType<IFooOrBar> fooBarType = CreateType(new UnionType<IFooOrBar>(),
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
