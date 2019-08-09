using Xunit;
using HotChocolate.AspNetCore.GraphiQL;

namespace HotChocolate.AspNetCore
{
    public class GraphiQLOptionsTests
    {
        [Fact]
        public void Default_Values()
        {
            // arrange
            // act
            var options = new GraphiQLOptions();

            // act
            Assert.Equal("/graphiql", options.Path);
            Assert.Equal("/", options.QueryPath);
            Assert.Equal("/", options.SubscriptionPath);
        }

        [Fact]
        public void SetPath()
        {
            // arrange
            var options = new GraphiQLOptions();

            // act
            options.Path = "/foo";

            // act
            Assert.Equal("/foo", options.Path);
            Assert.Equal("/", options.QueryPath);
            Assert.Equal("/", options.SubscriptionPath);
        }

        [Fact]
        public void SetPath_Then_SetQueryPath()
        {
            // arrange
            var options = new GraphiQLOptions();

            // act
            options.Path = "/foo";
            options.QueryPath = "/bar";

            // act
            Assert.Equal("/foo", options.Path);
            Assert.Equal("/bar", options.QueryPath);
            Assert.Equal("/bar", options.SubscriptionPath);
        }

        [Fact]
        public void SetQueryPath()
        {
            // arrange
            var options = new GraphiQLOptions();

            // act
            options.QueryPath = "/foo";

            // act
            Assert.Equal("/foo/graphiql", options.Path);
            Assert.Equal("/foo", options.QueryPath);
            Assert.Equal("/foo", options.SubscriptionPath);
        }

        [Fact]
        public void SetQueryPath_Then_SetPath()
        {
            // arrange
            var options = new GraphiQLOptions();

            // act
            options.QueryPath = "/foo";
            options.Path = "/bar";

            // act
            Assert.Equal("/bar", options.Path);
            Assert.Equal("/foo", options.QueryPath);
            Assert.Equal("/foo", options.SubscriptionPath);
        }

        [Fact]
        public void SetQueryPath_Then_SetSubscriptionPath()
        {
            // arrange
            var options = new GraphiQLOptions();

            // act
            options.QueryPath = "/foo";
            options.SubscriptionPath = "/bar";

            // act
            Assert.Equal("/foo/graphiql", options.Path);
            Assert.Equal("/foo", options.QueryPath);
            Assert.Equal("/bar", options.SubscriptionPath);
        }

        [Fact]
        public void SetSubscriptionPath()
        {
            // arrange
            var options = new GraphiQLOptions();

            // act
            options.SubscriptionPath = "/foo";

            // act
            Assert.Equal("/graphiql", options.Path);
            Assert.Equal("/", options.QueryPath);
            Assert.Equal("/foo", options.SubscriptionPath);
        }

        [Fact]
        public void SetSubscriptionPath_Then_SetQueryPath()
        {
            // arrange
            var options = new GraphiQLOptions();

            // act
            options.SubscriptionPath = "/foo";
            options.QueryPath = "/bar";

            // act
            Assert.Equal("/bar/graphiql", options.Path);
            Assert.Equal("/bar", options.QueryPath);
            Assert.Equal("/foo", options.SubscriptionPath);
        }
    }
}
