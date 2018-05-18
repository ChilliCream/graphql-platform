using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Setup
{
    public class SchemaConfigurationTests
    {
        [Fact]
        public async Task BindResolverCollectionToObjectType()
        {
            // arrange
            DummyObjectType dummyObjectType = new DummyObjectType();

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Service<DummyObjectTypeResolver>())
               .Returns(new DummyObjectTypeResolver());
            resolverContext.Setup(t => t.Parent<DummyObjectType>())
               .Returns(dummyObjectType);

            StringType stringType = new StringType();

            ObjectType objectType = new ObjectType(new ObjectTypeConfig
            {
                Name = "Dummy",
                Fields = () => new Dictionary<string, Field>
                {
                    {
                        "bar",
                        new Field(new FieldConfig
                        {
                            Name = "bar",
                            Type = () => stringType
                        })
                    }
                }
            });

            SchemaContext schemaContext = new SchemaContext(
                new INamedType[] { stringType, objectType },
                new Dictionary<string, ResolveType>(), null);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindType<DummyObjectType>().To("Dummy");
            configuration.BindResolver<DummyObjectTypeResolver>().To("Dummy")
                .Resolve("bar").With(t => t.GetFooBar(It.Is<DummyObjectType>()));
            configuration.Commit(schemaContext);

            // assert
            FieldResolverDelegate fieldResolver = schemaContext.CreateResolver("Dummy", "bar");
            object result = fieldResolver(resolverContext.Object, CancellationToken.None);
            Assert.Equal(dummyObjectType.Bar, result);
        }

        [Fact]
        public async Task BindObjectTypeAsResolver()
        {
            // arrange
            DummyObjectType dummyObjectType = new DummyObjectType();

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Parent<DummyObjectType>())
               .Returns(dummyObjectType);

            StringType stringType = new StringType();

            ObjectType objectType = new ObjectType(new ObjectTypeConfig
            {
                Name = "Dummy",
                Fields = () => new Dictionary<string, Field>
                {
                    {
                        "bar",
                        new Field(new FieldConfig
                        {
                            Name = "bar",
                            Type = () => stringType
                        })
                    }
                }
            });

            SchemaContext schemaContext = new SchemaContext(
                new INamedType[] { stringType, objectType },
                new Dictionary<string, ResolveType>(), null);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindType<DummyObjectType>().To("Dummy");
            configuration.Commit(schemaContext);

            // assert
            FieldResolverDelegate fieldResolver = schemaContext.CreateResolver("Dummy", "bar");
            object result = fieldResolver(resolverContext.Object, CancellationToken.None);
            Assert.Equal(dummyObjectType.Bar, result);
        }

        [Fact]
        public async Task BindMethodAsFieldImplicitly()
        {
            // arrange
            DummyObjectType dummyObjectType = new DummyObjectType();

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Parent<DummyObjectType>())
               .Returns(dummyObjectType);

            ScalarType stringType = new StringType();
            ObjectType objectType = new ObjectType(new ObjectTypeConfig
            {
                Name = "Dummy",
                Fields = () => new Dictionary<string, Field>
                {
                    {
                        "bar2",
                        new Field(new FieldConfig
                        {
                            Name = "bar2",
                            Type = () => stringType
                        })
                    }
                }
            });

            SchemaContext schemaContext = new SchemaContext(
                new INamedType[] { stringType, objectType },
                new Dictionary<string, ResolveType>(), null);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindType<DummyObjectType>().To("Dummy");
            configuration.Commit(schemaContext);

            // assert
            FieldResolverDelegate fieldResolver = schemaContext.CreateResolver("Dummy", "bar2");
            object result = fieldResolver(resolverContext.Object, CancellationToken.None);
            Assert.Equal(dummyObjectType.GetBar2(), result);
        }

    }

    public class DummyObjectType
    {
        public string Bar { get; } = "hello";

        public string GetBar2() => "world";
    }

    public class DummyObjectTypeResolver
    {
        public string GetFooBar(DummyObjectType objectType)
        {
            return objectType.Bar;
        }
    }
}
