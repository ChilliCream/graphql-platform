using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectTypeTests
        : TypeTestBase
    {
        [Fact]
        public void InputObjectType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()
                    .Field("bar")
                    .Type<StringType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void InputObjectType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))
                    .Field("bar")
                    .Type<StringType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericInputObjectType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType<SimpleInput>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericInputObjectType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType<SimpleInput>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))));

                c.Options.StrictValidation = false;
            });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void Initialize_IgnoreProperty_PropertyIsNotInSchemaType()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Field(f => f.Id).Ignore());

            // assert
            fooType = CreateType(fooType);
            Assert.Collection(fooType.Fields,
                t => Assert.Equal("name", t.Name));
        }

        [Fact]
        public void ParseLiteral()
        {
            // arrange
            Schema schema = Create();
            InputObjectType object1Type =
                schema.GetType<InputObjectType>("Object1");
            ObjectValueNode literal = CreateObjectLiteral();

            // act
            object obj = object1Type.ParseLiteral(literal);

            // assert
            Assert.IsType<SerializationInputObject1>(obj);
            obj.MatchSnapshot();
        }

        [Fact]
        public void EnsureInputObjectTypeKindIsCorret()
        {
            // arrange
            Schema schema = Create();
            InputObjectType object1Type =
                schema.GetType<InputObjectType>("Object1");

            // act
            TypeKind kind = object1Type.Kind;

            // assert
            Assert.Equal(TypeKind.InputObject, kind);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_NameArgs()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive("foo").Field(f => f.Id).Directive("foo"));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive(new NameString("foo"))
                    .Field(f => f.Id)
                    .Directive(new NameString("foo")));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());


            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive(new DirectiveNode("foo"))
                    .Field(f => f.Id)
                    .Directive(new DirectiveNode("foo")));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive(new FooDirective())
                    .Field(f => f.Id)
                    .Directive(new FooDirective()));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive<FooDirective>()
                    .Field(f => f.Id)
                    .Directive<FooDirective>());

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_NameArgs()
        {
            // arrange
            // act
            var fooType = new InputObjectType(
                d => d.Directive("foo")
                    .Field("id")
                    .Type<StringType>()
                    .Directive("foo"));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive(new NameString("foo"))
                    .Field("id")
                    .Type<StringType>()
                    .Directive(new NameString("foo")));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            var fooType = new InputObjectType(
                d => d.Directive(new DirectiveNode("foo"))
                    .Field("id")
                    .Type<StringType>()
                    .Directive(new DirectiveNode("foo")));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            var fooType = new InputObjectType(
                d => d.Directive(new FooDirective())
                    .Field("id")
                    .Type<StringType>()
                    .Directive(new FooDirective()));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            var fooType = new InputObjectType(
                d => d.Directive<FooDirective>()
                    .Field("id")
                    .Type<StringType>()
                    .Directive<FooDirective>());

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        private static ObjectValueNode CreateObjectLiteral()
        {
            return new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("foo",
                    new ObjectValueNode(new List<ObjectFieldNode>())),
                new ObjectFieldNode("bar",
                    new StringValueNode("123"))
            });
        }

        public Schema Create()
        {
            return Schema.Create(c =>
            {
                c.Options.StrictValidation = false;

                c.RegisterType(
                    new InputObjectType<SerializationInputObject1>(d =>
                    {
                        d.Name("Object1");
                        d.Field(t => t.Foo)
                            .Type<InputObjectType<SerializationInputObject2>>();
                        d.Field(t => t.Bar).Type<StringType>();
                    }));

                c.RegisterType(new InputObjectType<SerializationInputObject2>(
                    d =>
                    {
                        d.Name("Object2");
                        d.Field(t => t.FooList)
                            .Type<NonNullType<ListType<InputObjectType<
                                SerializationInputObject1>>>>();
                    }));
            });
        }
    }

    public class SimpleInput
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SerializationInputObject1
    {
        public SerializationInputObject2 Foo { get; set; }
        public string Bar { get; set; } = "Bar";
    }

    public class SerializationInputObject2
    {
        public List<SerializationInputObject1> FooList { get; set; } =
            new List<SerializationInputObject1>
        {
            new SerializationInputObject1()
        };
    }

    public class FooDirectiveType
        : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor.Name("foo");
            descriptor.Location(DirectiveLocation.InputObject)
                .Location(DirectiveLocation.InputFieldDefinition);
        }
    }

    public class FooDirective { }
}
