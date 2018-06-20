using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
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
            StringType scalarType = new StringType();

            Parser parser = new Parser();
            DocumentNode document = parser.Parse(
                "type Simple { a: String b: [String] }");
            ObjectTypeDefinitionNode objectTypeDefinition = document
                .Definitions.OfType<ObjectTypeDefinitionNode>().First();
            DelegateResolverBinding resolverBinding = new DelegateResolverBinding(
                "Simple", "a",
                (c, r) => "hello");

            ServiceManager serviceManager = new ServiceManager();
            SchemaContext context = new SchemaContext(serviceManager);
            context.Types.RegisterType(scalarType);
            context.Resolvers.RegisterResolver(resolverBinding);

            // act
            ObjectTypeFactory factory = new ObjectTypeFactory();
            ObjectType objectType = factory.Create(objectTypeDefinition);
            context.Types.RegisterType(objectType);

            ((INeedsInitialization)objectType).RegisterDependencies(context, error => { });
            context.CompleteTypes();

            // assert
            Assert.Equal("Simple", objectType.Name);
            Assert.Equal(2, objectType.Fields.Count);
            Assert.True(objectType.Fields.ContainsKey("a"));
            Assert.True(objectType.Fields.ContainsKey("b"));
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

            ServiceManager serviceManager = new ServiceManager();
            SchemaContext context = new SchemaContext(serviceManager);
            SchemaConfiguration configuration = new SchemaConfiguration(
                serviceManager.RegisterServiceProvider,
                context.Types);
            configuration.RegisterType(new ObjectType(d =>
                d.Name("A").Field("a").Type<StringType>()));
            configuration.RegisterType(new ObjectType(d =>
                d.Name("B").Field("a").Type<StringType>()));

            // act
            UnionTypeFactory factory = new UnionTypeFactory();
            UnionType unionType = factory.Create(unionTypeDefinition);
            configuration.RegisterType(unionType);

            TypeFinalizer typeFinalizer = new TypeFinalizer(configuration);
            typeFinalizer.FinalizeTypes(context);

            // assert
            Assert.Equal("X", unionType.Name);
            Assert.Equal(2, unionType.Types.Count);
            Assert.Equal("A", unionType.Types.First().Key);
            Assert.Equal("B", unionType.Types.Last().Key);
        }
    }
}
