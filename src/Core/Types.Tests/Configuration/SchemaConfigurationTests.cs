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
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType(d =>
                {
                    d.Name("TestObjectA");
                    d.Field("a").Type<StringType>();
                    d.Field("b").Type<StringType>();
                }));
                c.BindResolver<TestResolverCollectionA>().To<TestObjectA>();
            });

            // assert
            ObjectType type = schema.GetType<ObjectType>("TestObjectA");
            Assert.NotNull(type.Fields["a"].Resolver);
            Assert.NotNull(type.Fields["b"].Resolver);
        }

        [Fact]
        public void BindResolverCollectionToObjectTypeExplicitly()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>(
                MockBehavior.Strict);
            resolverContext.Setup(t => t.Parent<TestObjectA>())
                .Returns(new TestObjectA());
            resolverContext.Setup(t => t.Resolver<TestResolverCollectionA>())
                .Returns(new TestResolverCollectionA());
            resolverContext.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType(d =>
                {
                    d.Name("TestObjectA");
                    d.Field("a").Type<StringType>();
                    d.Field("b").Type<StringType>();
                }));

                c.BindResolver<TestResolverCollectionA>(
                    BindingBehavior.Explicit)
                    .To<TestObjectA>()
                    .Resolve(t => t.A)
                    .With(t => t.GetA(default))
                    .Resolve(t => t.B)
                    .With(t => t.GetA(default));
            });

            // assert
            ObjectType type = schema.GetType<ObjectType>("TestObjectA");
            Assert.NotNull(type.Fields["a"].Resolver);
            Assert.NotNull(type.Fields["b"].Resolver);

            Assert.Equal("a_dummy", type.Fields["a"].Resolver(
                resolverContext.Object).Result);
        }

        [Fact]
        public void BindResolverCollectionToObjectTypeViaName()
        {
            // arrange
            var dummyObjectType = new TestObjectB();

            var resolverContext = new Mock<IResolverContext>(MockBehavior.Strict);
            resolverContext.Setup(t => t.Resolver<TestResolverCollectionB>())
               .Returns(new TestResolverCollectionB());
            resolverContext.Setup(t => t.Parent<TestObjectB>())
               .Returns(dummyObjectType);
            resolverContext.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType(new ObjectType(d =>
                {
                    d.Name("Dummy");
                    d.Field("bar").Type<StringType>();
                }));

                c.BindType<TestObjectB>().To("Dummy");
                c.BindResolver<TestResolverCollectionB>().To("Dummy")
                    .Resolve("bar").With(t => t.GetFooBar(default));
            });

            // assert
            ObjectType dummy = schema.GetType<ObjectType>("Dummy");
            FieldResolverDelegate fieldResolver = dummy.Fields["bar"].Resolver;
            object result = fieldResolver(resolverContext.Object).Result;
            Assert.Equal(dummyObjectType.Bar, result);
        }

        [Fact]
        public void DeriveResolverFromObjectTypeProperty()
        {
            // arrange
            var dummyObjectType = new TestObjectB();

            var resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Parent<TestObjectB>())
               .Returns(dummyObjectType);
            resolverContext.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            string source = @"
                type Dummy { bar: String }
            ";


            // act
            ISchema schema = Schema.Create(source, c =>
            {
                c.RegisterQueryType<DummyQuery>();
                c.BindType<TestObjectB>().To("Dummy");
            });

            // assert
            ObjectType dummy = schema.GetType<ObjectType>("Dummy");
            FieldResolverDelegate fieldResolver = dummy.Fields["bar"].Resolver;
            object result = fieldResolver(resolverContext.Object).Result;
            Assert.Equal(dummyObjectType.Bar, result);
        }

        [Fact]
        public void DeriveResolverFromObjectTypeMethod()
        {
            // arrange
            var dummyObjectType = new TestObjectB();

            var resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Parent<TestObjectB>())
               .Returns(dummyObjectType);

            string source = @"
                type Dummy { bar2: String }
            ";

            // act
            ISchema schema = Schema.Create(source, c =>
            {
                c.RegisterQueryType<DummyQuery>();
                c.BindType<TestObjectB>().To("Dummy");
            });

            // assert
            ObjectType dummy = schema.GetType<ObjectType>("Dummy");
            FieldResolverDelegate fieldResolver = dummy.Fields["bar2"].Resolver;
            object result = fieldResolver(resolverContext.Object).Result;
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

    public class DummyQuery
    {
        public string Foo { get; set; }
    }
}