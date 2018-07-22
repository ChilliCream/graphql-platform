using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
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

            ObjectType dummyType = new ObjectType(d =>
            {
                d.Name("TestObjectA");
                d.Field("a").Type<StringType>();
                d.Field("b").Type<StringType>();
            });

            schemaContext.Types.RegisterType(dummyType);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration(
                schemaContext.RegisterServiceProvider,
                schemaContext.Types);
            configuration.BindResolver<TestResolverCollectionA>().To<TestObjectA>();

            TypeFinalizer typeFinalizer = new TypeFinalizer(configuration);
            typeFinalizer.FinalizeTypes(schemaContext, null);
            bool hasErrors = typeFinalizer.Errors.Any();

            // assert
            Assert.False(hasErrors);
            Assert.NotNull(schemaContext.Resolvers.GetResolver("TestObjectA", "a"));
            Assert.NotNull(schemaContext.Resolvers.GetResolver("TestObjectA", "b"));
            Assert.Null(schemaContext.Resolvers.GetResolver("Dummy", "x"));
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

            ObjectType dummyType = new ObjectType(d =>
            {
                d.Name("TestObjectA");
                d.Field("a").Type<StringType>().Argument("a", a => a.Type<StringType>());
                d.Field("b").Type<StringType>();
            });

            schemaContext.Types.RegisterType(dummyType);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration(
                schemaContext.RegisterServiceProvider,
                schemaContext.Types);
            configuration
                .BindResolver<TestResolverCollectionA>(BindingBehavior.Explicit)
                .To<TestObjectA>()
                .Resolve(t => t.A)
                .With(t => t.GetA(default, default));

            TypeFinalizer typeFinalizer = new TypeFinalizer(configuration);
            typeFinalizer.FinalizeTypes(schemaContext, null);
            bool hasErrors = typeFinalizer.Errors.Any();

            // assert
            Assert.True(hasErrors);

            FieldResolverDelegate resolver = schemaContext.Resolvers.GetResolver("TestObjectA", "a");
            Assert.NotNull(resolver);
            Assert.Equal("a_dummy_a", resolver(resolverContext.Object, CancellationToken.None));
            Assert.Null(schemaContext.Resolvers.GetResolver("TestObjectA", "b"));
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

            ObjectType objectType = new ObjectType(d =>
            {
                d.Name("Dummy");
                d.Field("bar").Type<StringType>();
            });

            SchemaContext schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(objectType);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration(
                schemaContext.RegisterServiceProvider,
                schemaContext.Types);
            configuration.BindType<TestObjectB>().To("Dummy");
            configuration.BindResolver<TestResolverCollectionB>().To("Dummy")
                .Resolve("bar").With(t => t.GetFooBar(default));

            TypeFinalizer typeFinalizer = new TypeFinalizer(configuration);
            typeFinalizer.FinalizeTypes(schemaContext, null);
            bool hasErrors = typeFinalizer.Errors.Any();

            // assert
            Assert.False(hasErrors);

            FieldResolverDelegate fieldResolver = schemaContext.Resolvers.GetResolver("Dummy", "bar");
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

            ObjectType objectType = new ObjectType(d =>
            {
                d.Name("Dummy");
                d.Field("bar").Type<StringType>();
            });

            SchemaContext schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(objectType);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration(
               schemaContext.RegisterServiceProvider,
               schemaContext.Types);
            configuration.BindType<TestObjectB>().To("Dummy");

            TypeFinalizer typeFinalizer = new TypeFinalizer(configuration);
            typeFinalizer.FinalizeTypes(schemaContext, null);
            bool hasErrors = typeFinalizer.Errors.Any();

            // assert
            Assert.False(hasErrors);

            FieldResolverDelegate fieldResolver = schemaContext.Resolvers.GetResolver("Dummy", "bar");
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

            ObjectType objectType = new ObjectType(d =>
            {
                d.Name("Dummy");
                d.Field("bar2").Type<StringType>();
            });

            SchemaContext schemaContext = new SchemaContext();
            schemaContext.Types.RegisterType(objectType);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration(
                schemaContext.RegisterServiceProvider,
                schemaContext.Types);
            configuration.BindType<TestObjectB>().To("Dummy");

            TypeFinalizer typeFinalizer = new TypeFinalizer(configuration);
            typeFinalizer.FinalizeTypes(schemaContext, null);
            bool hasErrors = typeFinalizer.Errors.Any();

            // assert
            Assert.False(hasErrors);

            FieldResolverDelegate fieldResolver = schemaContext.Resolvers.GetResolver("Dummy", "bar2");
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
