using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Factories;
using Xunit;

namespace HotChocolate.Types.Factories
{
    public class ObjectTypeFactoryTests
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
            SchemaContext context = new SchemaContext();
            context.Types.RegisterType(scalarType);

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

            /*
        [Fact]
        public void CreateUnion()
        {
            // arrange
            DocumentNode document = Parser.Default.Parse(
                "union X = A | B");
            UnionTypeDefinitionNode unionTypeDefinition = document
                .Definitions.OfType<UnionTypeDefinitionNode>().First();

            SchemaContext context = new SchemaContext(
                new[] { new StringType() });
            SchemaConfiguration schemaConfiguration = new SchemaConfiguration();
            schemaConfiguration.RegisterType(c => new ObjectTypeConfig
            {
                Name = "A",
                Fields = new[]
                {
                    new Field(new FieldConfig
                    {
                        Name = "a",
                        Type = () => c.StringType()
                    })
                }
            });
            schemaConfiguration.RegisterType(c => new ObjectTypeConfig
            {
                Name = "B",
                Fields = new[]
                {
                    new Field(new FieldConfig
                    {
                        Name = "a",
                        Type = () => c.StringType()
                    })
                }
            });
            schemaConfiguration.Commit(context);

            // act
            UnionTypeFactory factory = new UnionTypeFactory();
            UnionType unionType = factory.Create(context, unionTypeDefinition);
            ((INeedsInitialization)unionType).CompleteInitialization(error => { });

            // assert
            Assert.Equal("X", unionType.Name);
            Assert.Equal(2, unionType.Types.Count);
            Assert.Equal("A", unionType.Types.First().Key);
            Assert.Equal("B", unionType.Types.Last().Key);
        }
         */
    }
}
