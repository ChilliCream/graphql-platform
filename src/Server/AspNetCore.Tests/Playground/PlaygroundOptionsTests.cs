using Xunit;
using HotChocolate.AspNetCore.Playground;
using System;

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

        [Fact]
        public void Path_Set_To_Empty_ArgumentException()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            Action action = () => options.Path = default;

            // act
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void QueryPath_Set_To_Empty_ArgumentException()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            Action action = () => options.QueryPath = default;

            // act
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SubscriptionPath_Set_To_Empty_ArgumentException()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            Action action = () => options.SubscriptionPath = default;

            // act
            Assert.Throws<ArgumentException>(action);
        }
    }
}
