using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class EnumTypeTests
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
                    .Directive(new DirectiveNode("bar"))));

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
            Assert.True(type.TryGetValue("BAR1", out object value));
            Assert.Equal(Foo.Bar1, value);
            Assert.True(type.TryGetValue("BAR2", out value));
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
                    d.BindItems(BindingBehavior.Explicit);
                    d.Item(Foo.Bar1);
                }));
                c.Options.StrictValidation = false;
            });

            // assert
            EnumType type = schema.GetType<EnumType>("Foo");
            Assert.NotNull(type);
            Assert.True(type.TryGetValue("BAR1", out object value));
            Assert.Equal(Foo.Bar1, value);
            Assert.False(type.TryGetValue("BAR2", out value));
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
                    .Directive(new DirectiveNode("bar"))));

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
                    .Directive(new DirectiveNode("bar"))));

                c.Options.StrictValidation = false;
            });

            // assert
            schema.ToString().MatchSnapshot();
        }

        public enum Foo
        {
            Bar1,
            Bar2
        }
    }
}
