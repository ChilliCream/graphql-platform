using Xunit;
using HotChocolate.AspNetCore.Playground;

namespace HotChocolate.AspNetCore
{
    public class PlaygroundOptionsTests
    {
        [Fact]
        public void Default_Values()
        {
            // arrange
            // act
            var options = new PlaygroundOptions();

            // act
            Assert.Equal("/playground", options.Path);
            Assert.Equal("/", options.QueryPath);
            Assert.Equal("/", options.SubscriptionPath);
        }

        [Fact]
        public void SetPath()
        {
            // arrange
            var options = new PlaygroundOptions();

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
            var options = new PlaygroundOptions();

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
            var options = new PlaygroundOptions();

            // act
            options.QueryPath = "/foo";

            // act
            Assert.Equal("/foo/playground", options.Path);
            Assert.Equal("/foo", options.QueryPath);
            Assert.Equal("/foo", options.SubscriptionPath);
        }

        [Fact]
        public void SetQueryPath_Then_SetPath()
        {
            // arrange
            var options = new PlaygroundOptions();

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
            var options = new PlaygroundOptions();

            // act
            options.QueryPath = "/foo";
            options.SubscriptionPath = "/bar";

            // act
            Assert.Equal("/foo/playground", options.Path);
            Assert.Equal("/foo", options.QueryPath);
            Assert.Equal("/bar", options.SubscriptionPath);
        }

        [Fact]
        public void SetSubscriptionPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.SubscriptionPath = "/foo";

            // act
            Assert.Equal("/playground", options.Path);
            Assert.Equal("/", options.QueryPath);
            Assert.Equal("/foo", options.SubscriptionPath);
        }

        [Fact]
        public void SetSubscriptionPath_Then_SetQueryPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.SubscriptionPath = "/foo";
            options.QueryPath = "/bar";

            // act
            Assert.Equal("/bar/playground", options.Path);
            Assert.Equal("/bar", options.QueryPath);
            Assert.Equal("/foo", options.SubscriptionPath);
        }
    }
}
