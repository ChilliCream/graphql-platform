using System.Collections.Generic;
using System.Linq;
using ChilliCream.Testing;
using HotChocolate.Configuration;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectTypeTests
    {
        [Fact]
        public void Initialize_IgnoreProperty_PropertyIsNotInSchemaType()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Field(f => f.Id).Ignore());
            INeedsInitialization init = fooType;

            // assert
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
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
            obj.Snapshot();
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
        public void Initialize_AddDirectives_DirectivesAreAvailable()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            schemaContext.Directives.RegisterDirectiveType<FooDirectiveType>();

            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive("foo").Field(f => f.Id).Directive("foo"));

            // assert
            schemaContext.Types.RegisterType(fooType);
            INeedsInitialization init = fooType;
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);
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
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("foo");
            descriptor.Location(DirectiveLocation.InputObject)
                .Location(DirectiveLocation.InputFieldDefinition);
        }
    }
}
