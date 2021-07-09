﻿using System.Threading;
using System.Threading.Tasks;
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
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d =>
                {
                    d.Name("TestObjectA");
                    d.Field("a").Type<StringType>();
                    d.Field("b").Type<StringType>();
                })
                .AddResolver<TestResolverCollectionA>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("TestObjectA");
            Assert.NotNull(type.Fields["a"].Resolver);
            Assert.NotNull(type.Fields["b"].Resolver);
        }

        [Fact]
        public async Task DeriveResolverFromObjectTypeMethod()
        {
            // arrange
            var dummyObjectType = new TestObjectB();

            var resolverContext = new Mock<IResolverContext>();
            resolverContext.Setup(t => t.Parent<TestObjectB>())
               .Returns(dummyObjectType);

            var source = @"type Dummy { bar2: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<DummyQuery>()
                .AddDocumentFromString(source)
                .AddResolver<TestObjectB>("Dummy")
                .Create();

            // assert
            ObjectType dummy = schema.GetType<ObjectType>("Dummy");
            FieldResolverDelegate fieldResolver = dummy.Fields["bar2"].Resolver;
            var result = await fieldResolver!(resolverContext.Object);
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
