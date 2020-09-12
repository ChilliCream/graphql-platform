using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using Moq;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class EnumTypeTests
        : TypeTestBase
    {
        [Fact]
        public void EnumType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType(d => d
                    .Name(dep => dep.Name + "Enum")
                    .DependsOn<StringType>()
                    .Item("BAR")));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("StringEnum");
            Assert.NotNull(type);
        }

        [Fact]
        public void EnumType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType(d => d
                    .Name(dep => dep.Name + "Enum")
                    .DependsOn(typeof(StringType))
                    .Item("BAR")));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("StringEnum");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericEnumType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType<Foo>(d => d
                    .Name(dep => dep.Name + "Enum")
                    .DependsOn<StringType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("StringEnum");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericEnumType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType<Foo>(d => d
                    .Name(dep => dep.Name + "Enum")
                    .DependsOn(typeof(StringType))));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("StringEnum");
            Assert.NotNull(type);
        }

        [Fact]
        public void EnumType_WithDirectives()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterDirective(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.Enum)));

                c.RegisterType(new EnumType<Foo>(d => d
                    .Directive<DirectiveNode>(new DirectiveNode("bar"))));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Directives,
                t => Assert.Equal("bar", t.Type.Name));
        }

        [Fact]
        public void EnumType_WithDirectivesT()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterDirective(new DirectiveType<Bar>(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.Enum)));

                c.RegisterType(new EnumType<Foo>(d => d
                    .Directive<Bar>()));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Directives,
                t => Assert.Equal("bar", t.Type.Name));
        }

        [Fact]
        public void ImplicitEnumType_DetectEnumValues()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType<Foo>());
                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);
            Assert.True(type.TryGetRuntimeValue("BAR1", out object value));
            Assert.Equal(Foo.Bar1, value);
            Assert.True(type.TryGetRuntimeValue("BAR2", out value));
            Assert.Equal(Foo.Bar2, value);
        }

        [Fact]
        public void ExplicitEnumType_OnlyContainDeclaredValues()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType<Foo>(d =>
                {
                    d.BindValues(BindingBehavior.Explicit);
                    d.Item(Foo.Bar1);
                }));
                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);
            Assert.True(type.TryGetRuntimeValue("BAR1", out object value));
            Assert.Equal(Foo.Bar1, value);
            Assert.False(type.TryGetRuntimeValue("BAR2", out value));
            Assert.Null(value);
        }

        [Fact]
        public void ExplicitEnumType_OnlyContainDeclaredValues_2()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType<Foo>(d =>
                {
                    d.BindValuesImplicitly().BindValuesExplicitly();
                    d.Item(Foo.Bar1);
                }));
                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);
            Assert.True(type.TryGetRuntimeValue("BAR1", out object value));
            Assert.Equal(Foo.Bar1, value);
            Assert.False(type.TryGetRuntimeValue("BAR2", out value));
            Assert.Null(value);
        }

        [Fact]
        public void ImplicitEnumType_OnlyBar1HasCustomName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType<Foo>(d =>
                {
                    d.Item(Foo.Bar1).Name("FOOBAR");
                }));
                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);

            Assert.Collection(type.Values,
                t =>
                {
                    Assert.Equal(Foo.Bar1, t.Value);
                    Assert.Equal("FOOBAR", t.Name);
                },
                t =>
                {
                    Assert.Equal(Foo.Bar2, t.Value);
                    Assert.Equal("BAR2", t.Name);
                });
        }

        [Fact]
        public void EnumType_WithNoValues()
        {
            // act
            Action a = () => Schema.Create(c =>
            {
                c.RegisterType<EnumType>();
            });

            // assert
            Assert.Throws<SchemaException>(a);
        }

        [Fact]
        public void EnsureEnumTypeKindIsCorrect()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType<Foo>());
                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Equal(TypeKind.Enum, type.Kind);
        }

        [Fact]
        public void EnumValue_ValueIsNull_SchemaException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddQueryType<Bar>()
                .AddType(new EnumType(d => d
                    .Name("Foo")
                    .Item<string>(null)))
                    .Create();

            // assert
#if NETCOREAPP2_1
            Assert.Throws<SchemaException>(action)
                .Errors.Single().Message.MatchSnapshot(
                    new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            Assert.Throws<SchemaException>(action)
                .Errors.Single().Message.MatchSnapshot();
#endif
        }

        [Fact]
        public void EnumValueT_ValueIsNull_SchemaException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddQueryType<Bar>()
                .AddType(new EnumType<Foo?>(d => d
                    .Name("Foo")
                    .Item(null)))
                    .Create();

            // assert
#if NETCOREAPP2_1
            Assert.Throws<SchemaException>(action)
                .Errors.Single().Message.MatchSnapshot(
                    new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            Assert.Throws<SchemaException>(action)
                .Errors.Single().Message.MatchSnapshot();
#endif
        }

        [Fact]
        public void EnumValue_WithDirectives()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterDirective(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)));

                c.RegisterType(new EnumType(d => d
                    .Name("Foo")
                    .Item("baz")
                    .Directive<DirectiveNode>(new DirectiveNode("bar"))));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Values,
                v => Assert.Collection(v.Directives,
                    t => Assert.Equal("bar", t.Type.Name)));
        }

        [Fact]
        public void EnumValue_WithDirectivesNameArgs()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterDirective(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)));

                c.RegisterType(new EnumType(d => d
                    .Name("Foo")
                    .Item("baz")
                    .Directive("bar", Array.Empty<ArgumentNode>())));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Values,
                v => Assert.Collection(v.Directives,
                    t => Assert.Equal("bar", t.Type.Name)));
        }

        [Fact]
        public void Serialize_EnumValue_WithDirectives()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterDirective(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)));

                c.RegisterType(new EnumType(d => d
                    .Name("Foo")
                    .Item("baz")
                    .Directive<DirectiveNode>(new DirectiveNode("bar"))));

                c.Options.StrictValidation = false;
            });

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void EnumValue_WithDirectivesT()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterDirective(new DirectiveType<Bar>(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)));

                c.RegisterType(new EnumType(d => d
                    .Name("Foo")
                    .Item("baz")
                    .Directive<Bar>()));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Values,
                v => Assert.Collection(v.Directives,
                    t => Assert.Equal("bar", t.Type.Name)));
        }

        [Fact]
        public void EnumValue_WithDirectivesTInstance()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterDirective(new DirectiveType<Bar>(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)));

                c.RegisterType(new EnumType(d => d
                    .Name("Foo")
                    .Item("baz")
                    .Directive<Bar>(new Bar())));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Values,
                v => Assert.Collection(v.Directives,
                    t => Assert.Equal("bar", t.Type.Name)));
        }

        [Fact]
        public void EnumValue_SetContextData()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new EnumType(d => d
                    .Name("Foo")
                    .Value("bar")
                    .Extend()
                    .OnBeforeCreate(def => def.ContextData["baz"] = "qux")));

                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Values,
                v => Assert.Collection(v.ContextData,
                    c =>
                    {
                        Assert.Equal("baz", c.Key);
                        Assert.Equal("qux", c.Value);
                    }));
        }

        [Fact]
        public void EnumValue_DefinitionIsNull_ArgumentNullException()
        {
            // arrange
            var completionContext = new Mock<ITypeCompletionContext>();

            // act
            Action action = () => new EnumValue(completionContext.Object, null!);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void EnumValue_ContextIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => new EnumValue(null!, new EnumValueDefinition());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void EnumValue_DefinitionValueIsNull_ArgumentNullException()
        {
            // arrange
            var completionContext = new Mock<ITypeCompletionContext>();

            // act
            void Action() => new EnumValue(completionContext.Object, new EnumValueDefinition());

            // assert
            Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void Deprecate_Obsolete_Values()
        {
            // act
            var schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType<FooObsolete>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Deprecate_Fields_With_Deprecated_Attribute()
        {
            // act
            var schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType<FooDeprecated>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void EnumType_That_Is_Bound_To_String_Should_Not_Interfere_With_Scalar()
        {
            SchemaBuilder.New()
                .AddQueryType<SomeQueryType>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void Recognize_GraphQLNameAttribute_On_EnumType_And_EnumValue()
        {
            SchemaBuilder.New()
               .AddEnumType<FooName>()
               .ModifyOptions(o => o.StrictValidation = false)
               .Create()
               .ToString()
               .MatchSnapshot();
        }

        public enum Foo
        {
            Bar1,
            Bar2
        }

        public class Bar { }

        public enum FooObsolete
        {
            Bar1,

            [Obsolete]
            Bar2
        }

        public enum FooDeprecated
        {
            Bar1,
            [GraphQLDeprecated("Baz.")]
            Bar2
        }

        [GraphQLName("Foo")]
        public enum FooName
        {
            Bar1,
            [GraphQLName("BAR_2")]
            Bar2
        }

        public class SomeQueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("a").Type<SomeEnumType>().Resolver("DEF");
                descriptor.Field("b").Type<StringType>().Resolver("StringResolver");
            }
        }

        public class SomeEnumType
            : EnumType<string>
        {
            protected override void Configure(IEnumTypeDescriptor<string> descriptor)
            {
                descriptor.Name("Some");
                descriptor.Value("ABC").Name("DEF");
            }
        }
    }
}
