using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Factories;
using Xunit;

namespace HotChocolate
{
    public class ObjectTypeFactoryTests
    {
        [Fact]
        public void CreateObjectTypeWithTwoFields()
        {
            // arrange
            StringType scalarType = new StringType();

            Parser parser = new Parser();
            DocumentNode document = parser.Parse(
                "type Simple { a: String b: [String] }");
            ObjectTypeDefinitionNode objectTypeDefinition = document
                .Definitions.OfType<ObjectTypeDefinitionNode>().First();
            FieldResolver fieldResolver = new FieldResolver(
                "Simple", "a",
                (c, r) => "hello");
            SchemaContext context = new SchemaContext(
                new[] { scalarType });
            context.RegisterResolvers(new[] { fieldResolver });

            // act
            ObjectTypeFactory factory = new ObjectTypeFactory();
            ObjectType objectType = factory.Create(context, objectTypeDefinition);
            ((ITypeInitializer)objectType).CompleteInitialization(error => { });

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
    }
}
