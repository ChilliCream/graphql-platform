using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class EnumTypeTests : TypeTestBase
    {
        [Fact]
        public void EnumType_DynamicName()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType(d => d
                    .Name(dep => dep.Name + "Enum")
                    .DependsOn<StringType>()
                    .Value("BAR"))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("StringEnum");
            Assert.NotNull(type);
        }

        [Fact]
        public void EnumType_GraphQLDescriptionAttribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType<DescriptionTestEnum>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("DescriptionTestEnum");
            Assert.Equal("TestDescription", type.Description);
        }

        [Fact]
        public void EnumType_DynamicName_NonGeneric()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType(d => d
                    .Name(dep => dep.Name + "Enum")
                    .DependsOn(typeof(StringType))
                    .Value("BAR"))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("StringEnum");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericEnumType_DynamicName()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType(d => d
                    .Name(dep => dep.Name + "Enum")
                    .DependsOn<StringType>())
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("StringEnum");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericEnumType_DynamicName_NonGeneric()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType(d => d
                    .Name(dep => dep.Name + "Enum")
                    .DependsOn(typeof(StringType)))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("StringEnum");
            Assert.NotNull(type);
        }

        [Fact]
        public void EnumType_WithDirectives()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.Enum)))
                .AddEnumType(d => d.Directive(new DirectiveNode("bar")))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Directives, t => Assert.Equal("bar", t.Type.Name));
        }

        [Fact]
        public void EnumType_WithDirectivesT()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddDirectiveType(new DirectiveType<Bar>(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.Enum)))
                .AddEnumType(d => d.Directive<Bar>())
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Collection(type.Directives,
                t => Assert.Equal("bar", t.Type.Name));
        }

        [Fact]
        public void ImplicitEnumType_DetectEnumValues()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType<Foo>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);
            Assert.True(type.TryGetRuntimeValue("BAR1", out var value));
            Assert.Equal(Foo.Bar1, value);
            Assert.True(type.TryGetRuntimeValue("BAR2", out value));
            Assert.Equal(Foo.Bar2, value);
        }

        [Fact]
        public void ExplicitEnumType_OnlyContainDeclaredValues()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType<Foo>(d =>
                {
                    d.BindValues(BindingBehavior.Explicit);
                    d.Value(Foo.Bar1);
                })
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);
            Assert.True(type.TryGetRuntimeValue("BAR1", out var value));
            Assert.Equal(Foo.Bar1, value);
            Assert.False(type.TryGetRuntimeValue("BAR2", out value));
            Assert.Null(value);
        }

        [Fact]
        public void ExplicitEnumType_OnlyContainDeclaredValues_2()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType<Foo>(d =>
                {
                    d.BindValuesImplicitly().BindValuesExplicitly();
                    d.Value(Foo.Bar1);
                })
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);
            Assert.True(type.TryGetRuntimeValue("BAR1", out var value));
            Assert.Equal(Foo.Bar1, value);
            Assert.False(type.TryGetRuntimeValue("BAR2", out value));
            Assert.Null(value);
        }

        [Fact]
        public void ImplicitEnumType_OnlyBar1HasCustomName()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType<Foo>(d =>
                {
                    d.Value(Foo.Bar1).Name("FOOBAR");
                })
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);

            Assert.Collection(
                type.Values,
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
            void Action() => SchemaBuilder.New().AddType<EnumType>().Create();

            // assert
            Assert.Throws<SchemaException>(Action);
        }

        [Fact]
        public void EnsureEnumTypeKindIsCorrect()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<EnumType<Foo>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.Equal(TypeKind.Enum, type.Kind);
        }

        [Fact]
        public void EnumValue_ValueIsNull_SchemaException()
        {
            // arrange
            // act
            void Action() => SchemaBuilder.New()
                .AddQueryType<Bar>()
                .AddType(new EnumType(d => d.Name("Foo")
                    .Value<string>(null)))
                .Create();

            // assert
#if NETCOREAPP2_1
            Assert.Throws<SchemaException>(Action)
                .Errors.Single().Message.MatchSnapshot(
                    new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            Assert.Throws<SchemaException>(Action)
                .Errors.Single().Message.MatchSnapshot();
#endif
        }

        [Fact]
        public void EnumValueT_ValueIsNull_SchemaException()
        {
            // arrange
            // act
            void Action() =>
                SchemaBuilder.New()
                    .AddQueryType<Bar>()
                    .AddType(new EnumType<Foo?>(d => d.Name("Foo")
                        .Value(null)))
                    .Create();

            // assert

            Exception ex =
                Assert.Throws<SchemaException>(Action)
                    .Errors.Single().Exception;

            Assert.Equal(
                "runtimeValue",
                Assert.IsType<ArgumentNullException>(ex).ParamName);
        }

        [Fact]
        public void EnumValue_WithDirectives()
        {
            // act
            ISchema schema = SchemaBuilder
                .New()
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)))
                .AddEnumType(d => d
                    .Name("Foo")
                    .Value("baz")
                    .Directive(new DirectiveNode("bar")))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

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
            ISchema schema = SchemaBuilder
                .New()
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)))
                .AddEnumType(d => d
                    .Name("Foo")
                    .Value("baz")
                    .Directive("bar", Array.Empty<ArgumentNode>()))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

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
            ISchema schema = SchemaBuilder
                .New()
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)))
                .AddEnumType(d => d
                    .Name("Foo")
                    .Value("baz")
                    .Directive(new DirectiveNode("bar")))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void EnumValue_WithDirectivesT()
        {
            // act
            ISchema schema = SchemaBuilder
                .New()
                .AddDirectiveType(new DirectiveType<Bar>(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)))
                .AddEnumType(d => d
                    .Name("Foo")
                    .Value("baz")
                    .Directive<Bar>())
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

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
            ISchema schema = SchemaBuilder
                .New()
                .AddDirectiveType(new DirectiveType<Bar>(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.EnumValue)))
                .AddEnumType(d => d
                    .Name("Foo")
                    .Value("baz")
                    .Directive(new Bar()))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

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
            ISchema schema = SchemaBuilder
                .New()
                .AddEnumType(d => d
                    .Name("Foo")
                    .Value("bar")
                    .Extend()
                    .OnBeforeCreate(def => def.ContextData["baz"] = "qux"))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

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
            void Action() => new EnumValue(completionContext.Object, null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void EnumValue_ContextIsNull_ArgumentNullException()
        {
            // arrange
            // act
            void Action() => new EnumValue(null!, new EnumValueDefinition());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
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
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
                .AddType<FooObsolete>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Deprecate_Fields_With_Deprecated_Attribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
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
                descriptor.Field("a").Type<SomeEnumType>().Resolve("DEF");
                descriptor.Field("b").Type<StringType>().Resolve("StringResolver");
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

        [GraphQLDescription("TestDescription")]
        public enum DescriptionTestEnum
        {
            Foo,
            Bar
        }
    }
}
