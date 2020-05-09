using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class InputUnionTypeTests
        : TypeTestBase
    {
        [Fact]
        public void InputUnionType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputUnionType(d => d
                    .Name(dep => dep.Name + "FooInput")
                    .DependsOn<StringType>()
                    .Type<FooInputType>()
                    .Type<BarInputType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            InputUnionType type = schema.GetType<InputUnionType>("StringFooInput");
            Assert.NotNull(type);
        }

        [Fact]
        public void InputUnionType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputUnionType(d => d
                    .Name(dep => dep.Name + "FooInput")
                    .DependsOn(typeof(StringType))
                    .Type<FooInputType>()
                    .Type<BarInputType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            InputUnionType type = schema.GetType<InputUnionType>("StringFooInput");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericInputUnionType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputUnionType<IFooOrBar>(d => d
                    .Name(dep => dep.Name + "FooInput")
                    .DependsOn<StringType>()));

                c.RegisterType(new FooInputType());
                c.RegisterType(new BarInputType());

                c.Options.StrictValidation = false;
            });

            // assert
            InputUnionType type = schema.GetType<InputUnionType>("StringFooInput");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericInputUnionType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputUnionType<IFooOrBar>(d => d
                    .Name(dep => dep.Name + "FooInput")
                    .DependsOn(typeof(StringType))));

                c.RegisterType(new FooInputType());
                c.RegisterType(new BarInputType());

                c.Options.StrictValidation = false;
            });

            // assert
            InputUnionType type = schema.GetType<InputUnionType>("StringFooInput");
            Assert.NotNull(type);
        }

        [Fact]
        public void DeclareInputUnion_ByProvidingExplicitTypeSet()
        {
            // arrange
            // act
            InputUnionType fooBarInputType = CreateType(new InputUnionType(d => d
                .Name("FooOrBar")
                .Type<FooInputType>()
                .Type<BarInputType>()));

            // assert
            Assert.Collection(fooBarInputType.Types.Values,
                t => Assert.Equal("FooInput", t.Name),
                t => Assert.Equal("BarInput", t.Name));
        }

        [Fact]
        public void DeclareInputUnion_InferTypeSetFromMarkerInterface()
        {
            // arrange
            // act
            InputUnionType<IFooOrBar> fooBarInputType = CreateType(
                new InputUnionType<IFooOrBar>(),
                b => b.AddTypes(new FooInputType(), new BarInputType()));

            // assert
            Assert.Collection(fooBarInputType.Types.Values,
                t => Assert.Equal("FooInput", t.Name),
                t => Assert.Equal("BarInput", t.Name));
        }

        [Fact]
        public void DeclareInputUnion_MarkerInterfaceAndTypeSet()
        {
            // arrange
            // act
            InputUnionType<IFooOrBar> fooBarInputType = CreateType(
                new InputUnionType<IFooOrBar>(c => c.Type<BazInputType>()),
                b => b.AddTypes(new FooInputType(), new BarInputType()));

            // assert
            Assert.Collection(fooBarInputType.Types.Values,
                t => Assert.Equal("BazInput", t.Name),
                t => Assert.Equal("FooInput", t.Name),
                t => Assert.Equal("BarInput", t.Name));
        }

        [Fact]
        public void InputUnionType_AddDirectives_NameArgs()
        {
            // arrange
            // act
            InputUnionType fooBarInputType = CreateType(new InputUnionType(d => d
                .Name("BarInputUnion")
                .Directive("foo")
                .Type<FooInputType>()
                .Type<BarInputType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarInputType.Directives["foo"]);
        }

        [Fact]
        public void InputUnionType_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            InputUnionType fooBarInputType = CreateType(new InputUnionType(d => d
                .Name("BarInputUnion")
                .Directive(new NameString("foo"))
                .Type<FooInputType>()
                .Type<BarInputType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarInputType.Directives["foo"]);
        }

        [Fact]
        public void InputUnionType_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            InputUnionType fooBarInputType = CreateType(new InputUnionType(d => d
                .Name("BarInputUnion")
                .Directive(new DirectiveNode("foo"))
                .Type<FooInputType>()
                .Type<BarInputType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarInputType.Directives["foo"]);
        }

        [Fact]
        public void InputUnionType_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            InputUnionType fooBarInputType = CreateType(new InputUnionType(d => d
                .Name("BarInputUnion")
                .Directive(new FooDirective())
                .Type<FooInputType>()
                .Type<BarInputType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarInputType.Directives["foo"]);
        }

        [Fact]
        public void InputUnionType_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            InputUnionType fooBarInputType = CreateType(new InputUnionType(d => d
                .Name("BarInputUnion")
                .Directive<FooDirective>()
                .Type<FooInputType>()
                .Type<BarInputType>()),
                b => b.AddDirectiveType<FooDirectiveType>());

            // assert
            Assert.NotEmpty(fooBarInputType.Directives["foo"]);
        }

        [Fact]
        public void ParseLiteral_WithTypeName_Foo()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()));
            });
            InputUnionType inputUnionType = schema.GetType<InputUnionType>("BarInputUnion");
            ObjectValueNode literal = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("fooField", new StringValueNode("123")),
                new ObjectFieldNode("__typename", new StringValueNode("FooInput"))
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Foo>(obj);
            Assert.Equal("123", (obj as Foo)?.FooField);
            obj.MatchSnapshot();
        }

        [Fact]
        public void ParseLiteral_WithTypeName_Bar()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()));
            });
            InputUnionType inputUnionType = schema.GetType<InputUnionType>("BarInputUnion");
            ObjectValueNode literal = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("barField", new StringValueNode("123")),
                new ObjectFieldNode("__typename", new StringValueNode("BarInput"))
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Bar>(obj);
            Assert.Equal("123", (obj as Bar)?.BarField);
            obj.MatchSnapshot();
        }

        [Fact]
        public void ParseLiteral_WithTypeName_Invalid()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()));
            });
            InputUnionType inputUnionType = schema.GetType<InputUnionType>("BarInputUnion");
            ObjectValueNode literal = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("barField", new StringValueNode("123")),
                new ObjectFieldNode("__typename", new StringValueNode("BarInput"))
            });

            // act 
            // assert 
            InputObjectSerializationException exception =
                Assert.Throws<InputObjectSerializationException>(
                    () => inputUnionType.ParseLiteral(literal));

            Assert.Equal(TypeResources.InputUnionType_UnableToResolveType, exception.Message);
        }

        public class FooInputType
            : InputObjectType<Foo>
        {
        }

        public class BarInputType
            : InputObjectType<Bar>
        {
        }

        public class BazInputType
            : InputObjectType<Baz>
        {
        }

        public class Foo
            : IFooOrBar
        {
            public string? FooField { get; set; }

            public string? SharedField { get; set; }

            public string? SharedFieldDifferentType { get; set; }

            public Bar? NestedField { get; set; }

            public Bar? NestedFieldDifferentType { get; set; }
        }

        public class Bar
            : IFooOrBar
        {
            public string? BarField { get; set; }

            public string? SharedField { get; set; }

            public int? SharedFieldDifferentType { get; set; }

            public Bar? NestedField { get; set; }

            public Foo? NestedFieldDifferentType { get; set; }
        }

        public class Baz
            : IFooOrBar
        {
            public string? BazField { get; set; }
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
                descriptor.Location(DirectiveLocation.InputUnion)
                    .Location(DirectiveLocation.FieldDefinition);
            }
        }

        public class FooDirective { }
    }
}
