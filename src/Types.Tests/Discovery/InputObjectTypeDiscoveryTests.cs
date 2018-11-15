using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Discovery
{
    public class InputObjectTypeDiscoveryTests
    {

        [Fact]
        public void Test()
        {
            // arrange
            // act
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
            });

            // assert
            IInputType fooInput = schema.GetType<INamedInputType>("FooInput");
            Assert.NotNull(fooInput);

            IInputType barInput = schema.GetType<INamedInputType>("BarInput");
            Assert.NotNull(barInput);
        }

        public class Query
        {
            public Foo GetFoo(Foo foo)
            {
                return foo;
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
    }
}
