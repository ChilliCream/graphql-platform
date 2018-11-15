using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Discovery
{
    public class SchemaTypeDiscoveryTests
    {
        [Fact]
        public void DiscoverInputArgumentTypes()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<QueryFieldArgument>();
            });

            // assert
            IInputType fooInput = schema.GetType<INamedInputType>("FooInput");
            Assert.NotNull(fooInput);

            IInputType barInput = schema.GetType<INamedInputType>("BarInput");
            Assert.NotNull(barInput);
        }

        [Fact]
        public void DiscoverOutputGraphFromMethod()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<QueryField>();
            });

            // assert
            var query = schema.GetType<ObjectType>("query");
            Assert.NotNull(query);
            Assert.Collection(query.Fields,
                t => Assert.Equal("foo", t.Name));

            var foo = schema.GetType<ObjectType>("Foo");
            Assert.NotNull(foo);

            var bar = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(foo);
        }

        [Fact]
        public void DiscoverOutputGraphFromProperty()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<QueryProperty>();
            });

            // assert
            var query = schema.GetType<ObjectType>("query");
            Assert.NotNull(query);
            Assert.Collection(query.Fields,
                t => Assert.Equal("foo", t.Name));

            var foo = schema.GetType<ObjectType>("Foo");
            Assert.NotNull(foo);

            var bar = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(foo);
        }

        [Fact]
        public void DiscoverOutputGraphAndIgnoreVoidMethods()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<QueryMethodVoid>();
            });

            // assert
            var query = schema.GetType<ObjectType>("query");
            Assert.NotNull(query);
            Assert.Collection(query.Fields,
                t => Assert.Equal("foo", t.Name));

            var foo = schema.GetType<ObjectType>("Foo");
            Assert.NotNull(foo);

            var bar = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(foo);
        }

        [Fact]
        public void InferEnumAsEnumType()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType<FooBar>();
                c.Options.StrictValidation = false;
            });

            // assert
            var fooBar = schema.GetType<EnumType>("FooBar");
            Assert.NotNull(fooBar);
            Assert.Collection(fooBar.Values,
                t => Assert.Equal("FOO", t.Name),
                t => Assert.Equal("BAR", t.Name));
        }

        public class QueryFieldArgument
        {
            public Foo GetFoo(Foo foo)
            {
                return foo;
            }
        }

        public class QueryField
        {
            public Foo GetFoo()
            {
                return null;
            }
        }

        public class QueryProperty
        {
            public Foo GetFoo { get; }
        }

        public class QueryMethodVoid
        {
            public Foo GetFoo()
            {
                return null;
            }

            public void GetBar()
            {
            }
        }

        public class Foo
        {
            public Bar Bar { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }

        public enum FooBar
        {
            Foo,
            Bar
        }
    }
}
