using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Configuration
{
    public class SchemaConfigurationTests
    {
        [Fact]
        public void BindResolverCollectionToObjectTypeImplicitly()
        {
            // arrange
            SchemaContext schemaContext = new SchemaContext();

            StringType stringType = new StringType();
            ObjectType dummyType = new ObjectType(new ObjectTypeConfig
            {
                Name = "TestObjectA",
                Fields = () => new Dictionary<string, Field>
                {
                    { "a", new Field(new FieldConfig{ Name= "a", Type = () => stringType }) },
                    { "b", new Field(new FieldConfig{ Name= "a", Type = () => stringType }) }
                }
            });

            schemaContext.RegisterType(stringType);
            schemaContext.RegisterType(dummyType);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindResolver<TestResolverCollectionA>().To<TestObjectA>();
            configuration.Commit(schemaContext);

            // assert
            Assert.NotNull(schemaContext.CreateResolver("TestObjectA", "a"));
            Assert.NotNull(schemaContext.CreateResolver("TestObjectA", "b"));
            Assert.Throws<InvalidOperationException>(
                () => schemaContext.CreateResolver("Dummy", "x"));
        }

        [Fact]
        public void BindResolverCollectionToObjectTypeExplicitly()
        {
            // arrange
            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            resolverContext.Setup(t => t.Parent<TestObjectA>()).Returns(new TestObjectA());
            resolverContext.Setup(t => t.Service<TestResolverCollectionA>())
                .Returns(new TestResolverCollectionA());
            resolverContext.Setup(t => t.Argument<string>("a")).Returns("foo");

            SchemaContext schemaContext = new SchemaContext();

            StringType stringType = new StringType();
            Func<Dictionary<string, InputField>> arguments = () => new Dictionary<string, InputField>
            {
                { "a", new InputField(new InputFieldConfig { Name = "a", Type = () => stringType }) }
            };
            ObjectType dummyType = new ObjectType(new ObjectTypeConfig
            {
                Name = "TestObjectA",
                Fields = () => new Dictionary<string, Field>
                {
                    { "a", new Field(new FieldConfig{ Name= "a", Type = () => stringType, Arguments = arguments}) },
                    { "b", new Field(new FieldConfig{ Name= "b", Type = () => stringType}) }
                }
            });

            schemaContext.RegisterType(stringType);
            schemaContext.RegisterType(dummyType);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration
                .BindResolver<TestResolverCollectionA>(BindingBehavior.Explicit)
                .To<TestObjectA>()
                .Resolve(t => t.A)
                .With(t => t.GetA(default, default));

            configuration.Commit(schemaContext);

            // assert
            FieldResolverDelegate resolver = schemaContext.CreateResolver("TestObjectA", "a");
            Assert.NotNull(resolver);
            Assert.Equal("a_dummy_a", resolver(resolverContext.Object, CancellationToken.None));
            Assert.Throws<InvalidOperationException>(
                () => schemaContext.CreateResolver("TestObjectA", "b"));
        }

        [Fact]
        public void BindResolverCollectionToObjectTypeViaName()
        {
            // arrange
            TestObjectB dummyObjectType = new TestObjectB();

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Service<TestResolverCollectionB>())
               .Returns(new TestResolverCollectionB());
            resolverContext.Setup(t => t.Parent<TestObjectB>())
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
                new INamedType[] { stringType, objectType });

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindType<TestObjectB>().To("Dummy");
            configuration.BindResolver<TestResolverCollectionB>().To("Dummy")
                .Resolve("bar").With(t => t.GetFooBar(It.Is<TestObjectB>()));
            configuration.Commit(schemaContext);

            // assert
            FieldResolverDelegate fieldResolver = schemaContext.CreateResolver("Dummy", "bar");
            object result = fieldResolver(resolverContext.Object, CancellationToken.None);
            Assert.Equal(dummyObjectType.Bar, result);
        }

        [Fact]
        public void DeriveResolverFromObjectTypeProperty()
        {
            // arrange
            TestObjectB dummyObjectType = new TestObjectB();

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Parent<TestObjectB>())
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
                new INamedType[] { stringType, objectType });

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindType<TestObjectB>().To("Dummy");
            configuration.Commit(schemaContext);

            // assert
            FieldResolverDelegate fieldResolver = schemaContext.CreateResolver("Dummy", "bar");
            object result = fieldResolver(resolverContext.Object, CancellationToken.None);
            Assert.Equal(dummyObjectType.Bar, result);
        }

        [Fact]
        public void DeriveResolverFromObjectTypeMethod()
        {
            // arrange
            TestObjectB dummyObjectType = new TestObjectB();

            Mock<IResolverContext> resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Parent<TestObjectB>())
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
                new INamedType[] { stringType, objectType });

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindType<TestObjectB>().To("Dummy");
            configuration.Commit(schemaContext);

            // assert
            FieldResolverDelegate fieldResolver = schemaContext.CreateResolver("Dummy", "bar2");
            object result = fieldResolver(resolverContext.Object, CancellationToken.None);
            Assert.Equal(dummyObjectType.GetBar2(), result);
        }
    }

    public class TestResolverCollectionA
    {
        public string GetA(TestObjectA dummy)
        {
            return "a_dummy";
        }

        public string GetA(TestObjectA dummy, string a)
        {
            return "a_dummy_a";
        }

        public string GetFoo(TestObjectA dummy)
        {
            return null;
        }

        public string B { get; set; }
    }

    public class TestObjectA
    {
        public string A { get; set; } = "a";
        public string B { get; set; } = "b";
    }

    public class TestResolverCollectionB
    {
        public string GetFooBar(TestObjectB objectType)
        {
            return objectType.Bar;
        }
    }

    public class TestObjectB
    {
        public string Bar { get; } = "hello";

        public string GetBar2() => "world";
    }
}
