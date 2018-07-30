using System.Linq;
using System.Threading;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
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
            var scalarType = new StringType();

            var parser = new Parser();
            DocumentNode document = parser.Parse(
                "type Simple { a: String b: [String] }");
            ObjectTypeDefinitionNode objectTypeDefinition = document
                .Definitions.OfType<ObjectTypeDefinitionNode>().First();
            var resolverBinding = new DelegateResolverBinding(
                "Simple", "a",
                (c, r) => "hello");

            var schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(scalarType);
            schemaContext.Resolvers.RegisterResolver(resolverBinding);

            // act
            var factory = new ObjectTypeFactory();
            ObjectType objectType = factory.Create(objectTypeDefinition);
            schemaContext.Types.RegisterType(objectType);

            var initializationContext = new TypeInitializationContext(
                schemaContext, error => { }, objectType, false);
            ((INeedsInitialization)objectType)
                .RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            // assert
            Assert.Equal("Simple", objectType.Name);
            Assert.Equal(3, objectType.Fields.Count);
            Assert.True(objectType.Fields.ContainsField("a"));
            Assert.True(objectType.Fields.ContainsField("b"));
            Assert.False(objectType.Fields["a"].Type.IsNonNullType());
            Assert.False(objectType.Fields["a"].Type.IsListType());
            Assert.True(objectType.Fields["a"].Type.IsScalarType());
            Assert.Equal("String", objectType.Fields["a"].Type.TypeName());
            Assert.False(objectType.Fields["b"].Type.IsNonNullType());
            Assert.True(objectType.Fields["b"].Type.IsListType());
            Assert.False(objectType.Fields["b"].Type.IsScalarType());
            Assert.Equal("String", objectType.Fields["b"].Type.TypeName());
            Assert.Equal("hello", (objectType.Fields["a"]
                .Resolver(null, CancellationToken.None)));
        }


        [Fact]
        public void CreateUnion()
        {
            // arrange
            DocumentNode document = Parser.Default.Parse(
                "union X = A | B");
            UnionTypeDefinitionNode unionTypeDefinition = document
                .Definitions.OfType<UnionTypeDefinitionNode>().First();

            var context = new SchemaContext();
            var configuration = new SchemaConfiguration(
                context.RegisterServiceProvider,
                context.Types);
            configuration.RegisterType(new ObjectType(d =>
                d.Name("A").Field("a").Type<StringType>()));
            configuration.RegisterType(new ObjectType(d =>
                d.Name("B").Field("a").Type<StringType>()));

            // act
            var factory = new UnionTypeFactory();
            UnionType unionType = factory.Create(unionTypeDefinition);
            configuration.RegisterType(unionType);

            var typeFinalizer = new TypeFinalizer(configuration);
            typeFinalizer.FinalizeTypes(context, null);

            // assert
            Assert.Equal("X", unionType.Name);
            Assert.Equal(2, unionType.Types.Count);
            Assert.Equal("A", unionType.Types.First().Key);
            Assert.Equal("B", unionType.Types.Last().Key);
        }

        [Fact]
        public void CreateEnum()
        {
            // arrange
            DocumentNode document = Parser.Default.Parse(
                "enum Abc { A B C }");
            EnumTypeDefinitionNode typeDefinition = document
                .Definitions.OfType<EnumTypeDefinitionNode>().First();

            var context = new SchemaContext();
            var configuration = new SchemaConfiguration(
                context.RegisterServiceProvider,
                context.Types);

            // act
            var factory = new EnumTypeFactory();
            EnumType enumType = factory.Create(typeDefinition);
            configuration.RegisterType(enumType);

            var typeFinalizer = new TypeFinalizer(configuration);
            typeFinalizer.FinalizeTypes(context, null);

            // assert
            Assert.Equal("Abc", enumType.Name);
            Assert.Collection(enumType.Values,
                t => Assert.Equal("A", t.Name),
                t => Assert.Equal("B", t.Name),
                t => Assert.Equal("C", t.Name));
        }
    }
}
