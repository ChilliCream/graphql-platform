using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
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
                    .Name((Func<INamedType, NameString>)(dep => (NameString)(dep.Name + "FooInput")))
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
                    .Name((Func<INamedType, NameString>)(dep => (NameString)(dep.Name + "FooInput")))
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
                .Directive<string>("foo")
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
                .Directive<DirectiveNode>(new DirectiveNode("foo"))
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
                .Directive<FooDirective>(new FooDirective())
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

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_WithoutTypeName_Foo()
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
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Foo>(obj);
            Assert.Equal("123", (obj as Foo)?.FooField);
            obj.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_WithoutTypeName_Bar()
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
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Bar>(obj);
            Assert.Equal("123", (obj as Bar)?.BarField);
            obj.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_WithoutTypeName_Default()
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
                new ObjectFieldNode("sharedField", new StringValueNode("123")),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Foo>(obj);
            Assert.Equal("123", (obj as Foo)?.SharedField);
            obj.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_SharedFieldDifferentType_Foo()
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
                new ObjectFieldNode("sharedFieldDifferentType", new StringValueNode("123")),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Foo>(obj);
            Assert.Equal("123", (obj as Foo)?.SharedFieldDifferentType);
            obj.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_SharedFieldDifferentType_Bar()
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
                new ObjectFieldNode("sharedFieldDifferentType", new IntValueNode(123)),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Bar>(obj);
            Assert.Equal(123, (obj as Bar)?.SharedFieldDifferentType);
            obj.MatchSnapshot();
        }

        [Fact]
        public void ParseLiteral_InvalidValueNode()
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
            IValueNode literal = new StringValueNode("123");

            // act 
            // assert 
            InputObjectSerializationException exception =
                Assert.Throws<InputObjectSerializationException>(
                    () => inputUnionType.ParseLiteral(literal));

            Assert.Equal(TypeResources.InputUnionType_CannotParseLiteral, exception.Message);
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_UnkownTypeStructure()
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
                new ObjectFieldNode("unkownField", new IntValueNode(123)),
            });

            // act 
            // assert 
            InputObjectSerializationException exception =
                Assert.Throws<InputObjectSerializationException>(
                    () => inputUnionType.ParseLiteral(literal));

            Assert.Equal(TypeResources.InputUnionType_UnableToResolveType, exception.Message);
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_DifferentTypesInSameList_Foo()
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

            ObjectValueNode foo = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("sharedFieldDifferentType", new StringValueNode("123")),
            });

            ObjectValueNode bar = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("sharedFieldDifferentType", new IntValueNode(123)),
            });

            ObjectValueNode literal = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode(
                    "unionList",
                    new ListValueNode(new List<IValueNode>(){ foo, bar})),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Foo>((obj as Foo)?.UnionList?[0]);
            Assert.IsType<Bar>((obj as Foo)?.UnionList?[1]);
            Assert.Equal("123", ((obj as Foo)?.UnionList?[0] as Foo)?.SharedFieldDifferentType);
            Assert.Equal(123, ((obj as Foo)?.UnionList?[1] as Bar)?.SharedFieldDifferentType);
            obj.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_SharedFieldDifferentType_Foo_Nested()
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
                new ObjectFieldNode(
                    "nestedFieldDifferentType",
                    new ObjectValueNode(
                        new List<ObjectFieldNode> {
                            new ObjectFieldNode(
                                "sharedFieldDifferentType",
                                new IntValueNode(123)) })),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Foo>(obj);
            Assert.Equal(123, (obj as Foo)?.NestedFieldDifferentType?.SharedFieldDifferentType);
            obj.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public void ParseLiteral_SharedFieldDifferentType_Bar_Nested()
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
                new ObjectFieldNode(
                    "nestedFieldDifferentType",
                    new ObjectValueNode(
                        new List<ObjectFieldNode> {
                            new ObjectFieldNode(
                                "sharedFieldDifferentType",
                                new StringValueNode("123")) })),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Bar>(obj);
            Assert.Equal("123", (obj as Bar)?.NestedFieldDifferentType?.SharedFieldDifferentType);
            obj.MatchSnapshot();
        }

        [Fact]
        public void ParseLiteral_UnkownTypeStructure_WithWrongTypename()
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
                new ObjectFieldNode("unkownField", new IntValueNode(123)),
                new ObjectFieldNode("__typename", new StringValueNode("BazInput")),
            });

            // act 
            // assert 
            InputObjectSerializationException exception =
                Assert.Throws<InputObjectSerializationException>(
                    () => inputUnionType.ParseLiteral(literal));

            Assert.Equal(TypeResources.InputUnionType_UnableToResolveType, exception.Message);
        }

        [Fact]
        public void ParseLiteral_UnkownTypeStructure_WithExistingTypename()
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
                new ObjectFieldNode("unkownField", new IntValueNode(123)),
                new ObjectFieldNode("__typename", new StringValueNode("FooInput")),
            });

            // act 
            // assert 
            InputObjectSerializationException exception =
                Assert.Throws<InputObjectSerializationException>(
                    () => inputUnionType.ParseLiteral(literal));

        }

        [Fact]
        public void ParseLiteral_DifferentTypesInSameList_Foo_WithTypename()
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

            ObjectValueNode foo = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("sharedFieldDifferentType", new StringValueNode("123")),
                new ObjectFieldNode("__typename", new StringValueNode("FooInput")),
            });

            ObjectValueNode bar = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("sharedFieldDifferentType", new IntValueNode(123)),
                new ObjectFieldNode("__typename", new StringValueNode("BarInput")),
            });

            ObjectValueNode literal = new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode(
                    "unionList",
                    new ListValueNode(new List<IValueNode>(){ foo, bar})),
                new ObjectFieldNode("__typename", new StringValueNode("FooInput")),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Foo>((obj as Foo)?.UnionList?[0]);
            Assert.IsType<Bar>((obj as Foo)?.UnionList?[1]);
            Assert.Equal("123", ((obj as Foo)?.UnionList?[0] as Foo)?.SharedFieldDifferentType);
            Assert.Equal(123, ((obj as Foo)?.UnionList?[1] as Bar)?.SharedFieldDifferentType);
            obj.MatchSnapshot();
        }

        [Fact]
        public void ParseLiteral_SharedFieldDifferentType_Foo_Nested_WithTypename()
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
                new ObjectFieldNode(
                    "nestedFieldDifferentType",
                    new ObjectValueNode(
                        new List<ObjectFieldNode> {
                            new ObjectFieldNode(
                                "sharedFieldDifferentType",
                                new IntValueNode(123)),
                            new ObjectFieldNode("__typename",
                                new StringValueNode("BarInput"))})),
                new ObjectFieldNode("__typename", new StringValueNode("FooInput")),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Foo>(obj);
            Assert.Equal(123, (obj as Foo)?.NestedFieldDifferentType?.SharedFieldDifferentType);
            obj.MatchSnapshot();
        }

        [Fact]
        public void ParseLiteral_SharedFieldDifferentType_Bar_Nested_WithTypename()
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
                new ObjectFieldNode(
                    "nestedFieldDifferentType",
                    new ObjectValueNode(
                        new List<ObjectFieldNode> {
                            new ObjectFieldNode(
                                "sharedFieldDifferentType",
                                new StringValueNode("123")),
                            new ObjectFieldNode("__typename",
                                new StringValueNode("FooInput")),})),
                new ObjectFieldNode("__typename", new StringValueNode("BarInput")),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.IsType<Bar>(obj);
            Assert.Equal("123", (obj as Bar)?.NestedFieldDifferentType?.SharedFieldDifferentType);
            obj.MatchSnapshot();
        }

        [Fact]
        public void ParseLiteral_ParseNullValueNode()
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
            NullValueNode literal = NullValueNode.Default;

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert 
            Assert.Null(obj);
        }

        [Fact]
        public void ParseLiteral_ParseNullValue()
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

            // act
            // assert 
            ArgumentNullException exception =
                Assert.Throws<ArgumentNullException>(
                    () => inputUnionType.ParseLiteral(null));
        }

        [Fact]
        public void ParseLiteral_NullValueInList()
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
                new ObjectFieldNode("__typename", new StringValueNode("FooInput")),
                new ObjectFieldNode(
                    "unionList",
                    new ListValueNode(new List<IValueNode>(){ NullValueNode.Default })),
            });

            // act
            object obj = inputUnionType.ParseLiteral(literal);

            // assert
            Assert.Null((obj as Foo)?.UnionList?[0]);
            obj.MatchSnapshot();
        }

        [Fact]
        public void EnsureInputObjectTypeKindIsCorret()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()));
            });
            InputUnionType inputUnionType = schema.GetType<InputUnionType>("BarInputUnion");

            // act
            TypeKind kind = inputUnionType.Kind;

            // assert
            Assert.Equal(TypeKind.InputUnion, kind);
        }

        [Fact]
        public void GenericInputObjectUnion_AddDirectives_NameArgs()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType<IFooOrBar>(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive("foo")));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void GenericInputObjectUnion_AddDirectives_NameArgs2()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType<IFooOrBar>(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive(new NameString("foo"))));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void GenericInputObjectUnion_AddDirectives_DirectiveNode()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType<IFooOrBar>(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive(new DirectiveNode("foo"))));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void GenericInputObjectUnion_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType<IFooOrBar>(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive(new FooDirective())));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void GenericInputObjectUnion_AddDirectives_DirectiveType()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType<IFooOrBar>(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive<FooDirective>()));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void InputObjectUnion_AddDirectives_NameArgs()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive("foo")));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void InputObjectUnion_AddDirectives_NameArgs2()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive(new NameString("foo"))));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void InputObjectUnion_AddDirectives_DirectiveNode()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive(new DirectiveNode("foo"))));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void InputObjectUnion_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive(new FooDirective())));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void InputObjectUnion_AddDirectives_DirectiveType()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()
                    .Directive<FooDirective>()));
            });

            // act
            InputUnionType fooType = schema.GetType<InputUnionType>("BarInputUnion");

            // assert
            Assert.NotEmpty(fooType.Directives["foo"]);
        }

        [Fact]
        public void IsInstanceOfType_ValueIsNull_True()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterDirective(new FooDirectiveType());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()));
            });

            InputUnionType fooType =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            bool result = fooType.IsInstanceOfType((object)null!);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_ValueIsBar_True()
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

            InputUnionType fooType =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            bool result = fooType.IsInstanceOfType(new Bar());

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_ValueIsFoo_True()
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

            InputUnionType fooType =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            bool result = fooType.IsInstanceOfType(new Foo());

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_ValueIsBaz_False()
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

            InputUnionType fooType =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            bool result = fooType.IsInstanceOfType(new Baz());

            // assert
            Assert.False(result);
        }

        [Fact]
        public void ParseValue_ValueIsNull()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            IValueNode valueNode = type.ParseValue((object)null!);

            // assert
            QuerySyntaxSerializer.Serialize(valueNode).MatchSnapshot();
        }

        [Fact]
        public void ParseValue_ValueIsBar()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            IValueNode valueNode = type.ParseValue(new Bar()
            {
                BarField = "123",
                SharedField = "123"
            });

            // assert
            QuerySyntaxSerializer.Serialize(valueNode).MatchSnapshot();
        }


        [Fact]
        public void Serialize_ValueIsNull()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            object serialized = type.Serialize(null);

            // assert
            Assert.Null(serialized);
        }

        [Fact]
        public void Serialize_ValueIsBar()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            object serialized = type.Serialize(
                new Bar
                {
                    BarField = "123",
                    SharedField = "123"
                });

            // assert
            serialized.MatchSnapshot();
        }

        [Fact]
        public void Serialize_ValueIsDictionary()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            object serialized = type.Serialize(
                new Dictionary<string, object>
                {
                    { "BarField", "123" }
                });

            // assert
            serialized.MatchSnapshot();
        }

        [Fact]
        public void Deserialize_ValueIsNull()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            bool result = type.TryDeserialize(null, out object value);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void Deserialize_ValueIsDictionary()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            bool result = type.TryDeserialize(
                new Dictionary<string, object>
                {
                    { "barField", "123" },
                    { "__typename", "BarInput" }
                },
                out object value);

            // assert
            Assert.Equal("123", Assert.IsType<Bar>(value).BarField);
        }

        [Fact]
        public void Deserialize_UnknownTypeName()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            // assert
            InputObjectSerializationException exception =
                Assert.Throws<InputObjectSerializationException>(
                    () => type.Deserialize(
                        new Dictionary<string, object>
                        {
                            { "barField", "123" },
                            { "__typename", "BazInput" }
                        }));

            Assert.Equal(TypeResources.InputUnionType_UnableToResolveType, exception.Message);
        }

        [Fact]
        public void Deserialize_NoTypeName()
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

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            // assert
            InputObjectSerializationException exception =
                Assert.Throws<InputObjectSerializationException>(
                    () => type.Deserialize(
                        new Dictionary<string, object>
                        {
                            { "barField", "123" },
                        }));

            Assert.Equal(TypeResources.InputUnionType_TypeNameNotSpecified, exception.Message);
        }

        [Fact]
        public void Deserialize_ClrType()
        {
            // arrange
            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType<Bar>(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()));
            });

            InputUnionType type =
                schema.GetType<InputUnionType>("BarInputUnion");

            // act
            // assert
            bool result = type.TryDeserialize(new Bar() { BarField = "123" }, out object value);

            // assert
            Assert.Equal("123", Assert.IsType<Bar>(value).BarField);
        }


        [Fact]
        public async Task Query_WithTypeName_Foo()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { fooField: ""123"", __typename: ""FooInput"" })  {
                        ... on Foo {
                            fooField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Query_WithTypeName_Bar()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { barField: ""fooField"", __typename: ""BarInput"" })  {
                        ... on Bar {
                            barField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Query_WithTypeName_Invalid()
        {
            // arrange 
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { barField: ""fooField"", __typename: ""BazInput"" })  {
                        ... on Bar {
                            barField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_WithoutTypeName_Foo()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { fooField: ""123"" })  {
                        ... on Foo {
                            fooField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_WithoutTypeName_Bar()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { barField: ""fooField"" })  {
                        ... on Bar {
                            barField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_WithoutTypeName_Default()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { sharedField: ""123"" })  {
                        ... on Foo {
                            sharedField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }


        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_SharedFieldDifferentType_Foo()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { sharedFieldWithDifferentType: ""123"" })  {
                        ... on Foo {
                            sharedField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_SharedFieldDifferentType_Bar()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { sharedFieldWithDifferentType: 123 })  {
                        ... on Foo {
                            sharedField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_UnkownTypeStructure()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { unkownField: 123 })  {
                        ... on Foo {
                            sharedField
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_DifferentTypesInSameList_Foo()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { unionList: [{ fooField: ""123"" },{ barField: ""123"" }] })  {
                        ... on Foo {
                           unionList {
                                ... on Foo {
                                    fooField
                                }
                                ... on Bar {
                                    barField
                                }
                            }       
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_SharedFieldDifferentType_Foo_Nested()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { nestedFieldDifferentType: {sharedFieldDifferentType:123} })  {
                        ... on Foo {
                            nestedFieldDifferentType { sharedFieldDifferentType }
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_SharedFieldDifferentType_Bar_Nested()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { nestedFieldDifferentType: {sharedFieldDifferentType:""123""} })  {
                        ... on Bar {
                            nestedFieldDifferentType { sharedFieldDifferentType }
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }


        [Fact]
        public async Task Query_ParseNullValue()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: null)  {
                        ... on Bar {
                            nestedFieldDifferentType { sharedFieldDifferentType }
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "does only work when we support shape matching")]
        public async Task Query_NullValueInList()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { unionList: [null,{ barField: ""123"" }] })  {
                        ... on Foo {
                           unionList {
                                ... on Foo {
                                    fooField
                                }
                                ... on Bar {
                                    barField
                                }
                            }       
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }



        [Fact]
        public async Task Query_NullValueInList_WithTypeName()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { unionList: [
                            null,
                            { barField: ""123"", __typename:""BarInput"" }] 
                        __typename:""FooInput""} )
                        {
                        ... on Foo {
                           unionList {
                                ... on Foo {
                                    fooField
                                }
                                ... on Bar {
                                    barField
                                }
                            }       
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }


        [Fact]
        public async Task Query_DifferentTypesInSameList_WithTypename_Foo()
        {
            // arrange
            IQueryExecutor executor = CreateQuerySchema();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    do(input: { unionList: [
                        { fooField: ""123"", __typename:""FooInput""},
                        { barField: ""123"", __typename:""BarInput""}]
                        __typename:""FooInput""})  {
                        ... on Foo {
                           unionList {
                                ... on Foo {
                                    fooField
                                }
                                ... on Bar {
                                    barField
                                }
                            }       
                        }
                    }
                }");

            // assert
            result.MatchSnapshot();
        }

        private IQueryExecutor CreateQuerySchema()
        {

            Schema schema = Schema.Create(x =>
            {
                x.Options.StrictValidation = false;
                x.RegisterQueryType(new ObjectType<Query>());
                x.RegisterType(new FooInputType());
                x.RegisterType(new BarInputType());
                x.RegisterType(new InputUnionType<IFooOrBar>(d => d
                    .Name("BarInputUnion")
                    .Type<FooInputType>()
                    .Type<BarInputType>()));
                x.RegisterType(new UnionType<IFooOrBar>(d => d
                    .Name("BarUnion")
                    .Type<ObjectType<Bar>>()
                    .Type<ObjectType<Foo>>()));
            });
            return schema.MakeExecutable();
        }

        public class Query
        {
            public IFooOrBar? GetDo(IFooOrBar? input) => input;
        }


        public class FooInputType
            : InputObjectType<Foo>
        {
            protected override void Configure(IInputObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(x => x.UnionList).Type<ListType<InputUnionType<IFooOrBar>>>();
            }
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

            public Bar? NestedFieldDifferentType { get; set; }

            public List<IFooOrBar>? UnionList { get; set; }
        }

        public class Bar
            : IFooOrBar
        {
            public string? BarField { get; set; }

            public string? SharedField { get; set; }

            public int? SharedFieldDifferentType { get; set; }

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
