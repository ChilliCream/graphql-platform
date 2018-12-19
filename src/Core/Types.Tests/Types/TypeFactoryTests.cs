using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Factories;
using Xunit;

namespace HotChocolate.Types
{
    public class TypeFactoryTests
    {
        [Fact]
        public void CreateObjectType()
        {
            // arrange
            ObjectTypeDefinitionNode typeDefinition =
                CreateTypeDefinition<ObjectTypeDefinitionNode>(@"
                    type Simple { a: String b: [String] }");

            var resolverBinding = new FieldResolver(
                "Simple", "a",
                c => Task.FromResult<object>("hello"));

            // act
            var factory = new ObjectTypeFactory();
            ObjectType type = factory.Create(typeDefinition);
            CompleteType(type,
                s => s.Resolvers.RegisterResolver(resolverBinding));

            // assert
            Assert.Equal("Simple", type.Name);
            Assert.Equal(3, type.Fields.Count);

            Assert.True(type.Fields.ContainsField("a"));
            Assert.False(type.Fields["a"].Type.IsNonNullType());
            Assert.False(type.Fields["a"].Type.IsListType());
            Assert.True(type.Fields["a"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["a"].Type.TypeName());

            Assert.True(type.Fields.ContainsField("b"));
            Assert.False(type.Fields["b"].Type.IsNonNullType());
            Assert.True(type.Fields["b"].Type.IsListType());
            Assert.False(type.Fields["b"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["b"].Type.TypeName());

            Assert.Equal("hello", (type.Fields["a"]
                .Resolver(null).Result));
        }

        [Fact]
        public void ObjectFieldDeprecationReason()
        {
            // arrange
            ObjectTypeDefinitionNode typeDefinition =
                CreateTypeDefinition<ObjectTypeDefinitionNode>(@"
                    type Simple {
                        a: String @deprecated(reason: ""reason123"")
                    }");

            // act
            var factory = new ObjectTypeFactory();
            ObjectType type = factory.Create(typeDefinition);
            CompleteType(type);

            // assert
            Assert.True(type.Fields["a"].IsDeprecated);
            Assert.Equal("reason123", type.Fields["a"].DeprecationReason);
        }

        [Fact]
        public void CreateInterfaceType()
        {
            // arrange
            InterfaceTypeDefinitionNode typeDefinition =
                CreateTypeDefinition<InterfaceTypeDefinitionNode>(
                    "interface Simple { a: String b: [String] }");

            // act
            var factory = new InterfaceTypeFactory();
            InterfaceType type = factory.Create(typeDefinition);
            CompleteType(type);

            // assert
            Assert.Equal("Simple", type.Name);
            Assert.Equal(2, type.Fields.Count);

            Assert.True(type.Fields.ContainsField("a"));
            Assert.False(type.Fields["a"].Type.IsNonNullType());
            Assert.False(type.Fields["a"].Type.IsListType());
            Assert.True(type.Fields["a"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["a"].Type.TypeName());

            Assert.True(type.Fields.ContainsField("b"));
            Assert.False(type.Fields["b"].Type.IsNonNullType());
            Assert.True(type.Fields["b"].Type.IsListType());
            Assert.False(type.Fields["b"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["b"].Type.TypeName());
        }

        [Fact]
        public void InterfaceFieldDeprecationReason()
        {
            // arrange
            InterfaceTypeDefinitionNode typeDefinition =
                CreateTypeDefinition<InterfaceTypeDefinitionNode>(@"
                    interface Simple {
                        a: String @deprecated(reason: ""reason123"")
                    }");

            // act
            var factory = new InterfaceTypeFactory();
            InterfaceType type = factory.Create(typeDefinition);
            CompleteType(type);

            // assert
            Assert.True(type.Fields["a"].IsDeprecated);
            Assert.Equal("reason123", type.Fields["a"].DeprecationReason);
        }

        [Fact]
        public void CreateUnion()
        {
            // arrange
            var objectTypeA = new ObjectType(d =>
                d.Name("A").Field("a").Type<StringType>());
            var objectTypeB = new ObjectType(d =>
                d.Name("B").Field("a").Type<StringType>());

            UnionTypeDefinitionNode typeDefinition =
                CreateTypeDefinition<UnionTypeDefinitionNode>(
                    "union X = A | B");

            // act
            var factory = new UnionTypeFactory();
            UnionType type = factory.Create(typeDefinition);
            CompleteType(type, s =>
            {
                s.Types.RegisterType(objectTypeA);
                s.Types.RegisterType(objectTypeB);
            });

            // assert
            Assert.Equal("X", type.Name);
            Assert.Equal(2, type.Types.Count);
            Assert.Equal("A", type.Types.First().Key);
            Assert.Equal("B", type.Types.Last().Key);
        }

        [Fact]
        public void CreateEnum()
        {
            // arrange
            EnumTypeDefinitionNode typeDefinition =
                CreateTypeDefinition<EnumTypeDefinitionNode>(
                    "enum Abc { A B C }");

            // act
            var factory = new EnumTypeFactory();
            EnumType type = factory.Create(typeDefinition);
            CompleteType(type);

            // assert
            Assert.Equal("Abc", type.Name);
            Assert.Collection(type.Values,
                t => Assert.Equal("A", t.Name),
                t => Assert.Equal("B", t.Name),
                t => Assert.Equal("C", t.Name));
        }

        [Fact]
        public void EnumValueDeprecationReason()
        {
            // arrange
            EnumTypeDefinitionNode typeDefinition =
                CreateTypeDefinition<EnumTypeDefinitionNode>(@"
                    enum Abc {
                        A
                        B @deprecated(reason: ""reason123"")
                        C
                    }");

            // act
            var factory = new EnumTypeFactory();
            EnumType type = factory.Create(typeDefinition);
            CompleteType(type);

            // assert
            EnumValue value = type.Values.FirstOrDefault(t => t.Name == "B");
            Assert.NotNull(value);
            Assert.True(value.IsDeprecated);
            Assert.Equal("reason123", value.DeprecationReason);
        }

        [Fact]
        public void CreateInputObjectType()
        {
            // arrange
            string schemaSdl = "input Simple { a: String b: [String] }";

            // act
            Schema schema = Schema.Create(
                schemaSdl,
                c =>
                {
                    c.Options.StrictValidation = false;
                    c.BindType<SimpleInputObject>().To("Simple")
                        .Field(t => t.Name).Name("a")
                        .Field(t => t.Friends).Name("b");
                });
            InputObjectType type = schema.GetType<InputObjectType>("Simple");

            // assert
            Assert.Equal("Simple", type.Name);
            Assert.Equal(2, type.Fields.Count);

            Assert.True(type.Fields.ContainsField("a"));
            Assert.False(type.Fields["a"].Type.IsNonNullType());
            Assert.False(type.Fields["a"].Type.IsListType());
            Assert.True(type.Fields["a"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["a"].Type.TypeName());

            Assert.True(type.Fields.ContainsField("b"));
            Assert.False(type.Fields["b"].Type.IsNonNullType());
            Assert.True(type.Fields["b"].Type.IsListType());
            Assert.False(type.Fields["b"].Type.IsScalarType());
            Assert.Equal("String", type.Fields["b"].Type.TypeName());
        }

        private void CompleteType(
            INamedType namedType,
            Action<SchemaContext> configure = null)
        {
            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(new StringType());
            schemaContext.Types.RegisterType(namedType);

            if (configure != null)
            {
                configure(schemaContext);
            }

            var initializationContext = new TypeInitializationContext(
                schemaContext, error => { }, namedType, false);
            ((INeedsInitialization)namedType)
                .RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();
        }

        private T CreateTypeDefinition<T>(string schema)
            where T : ISyntaxNode
        {
            var parser = new Parser();
            DocumentNode document = parser.Parse(schema);
            return document.Definitions.OfType<T>().First();
        }

        public class SimpleInputObject
        {
            public string Name { get; set; }
            public string[] Friends { get; set; }
        }
    }
}
