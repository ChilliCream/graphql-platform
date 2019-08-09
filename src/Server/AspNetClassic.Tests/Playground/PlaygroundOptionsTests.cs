using Xunit;
using HotChocolate.AspNetClassic.Playground;
using Microsoft.Owin;
using System;

namespace HotChocolate.AspNetClassic
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
            Assert.Equal("/playground", options.Path.ToString());
            Assert.Equal("/", options.QueryPath.ToString());
            Assert.Equal("/", options.SubscriptionPath.ToString());
        }

        [Fact]
        public void SetPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.Path = new PathString("/foo");

            // act
            Assert.Equal("/foo", options.Path.ToString());
            Assert.Equal("/", options.QueryPath.ToString());
            Assert.Equal("/", options.SubscriptionPath.ToString());
        }

        [Fact]
        public void SetPath_Then_SetQueryPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.Path = new PathString("/foo");
            options.QueryPath = new PathString("/bar");

            // act
            Assert.Equal("/foo", options.Path.ToString());
            Assert.Equal("/bar", options.QueryPath.ToString());
            Assert.Equal("/bar", options.SubscriptionPath.ToString());
        }

        [Fact]
        public void SetQueryPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.QueryPath = new PathString("/foo");

            // act
            Assert.Equal("/foo/playground", options.Path.ToString());
            Assert.Equal("/foo", options.QueryPath.ToString());
            Assert.Equal("/foo", options.SubscriptionPath.ToString());
        }

        [Fact]
        public void SetQueryPath_Then_SetPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.QueryPath = new PathString("/foo");
            options.Path = new PathString("/bar");

            // act
            Assert.Equal("/bar", options.Path.ToString());
            Assert.Equal("/foo", options.QueryPath.ToString());
            Assert.Equal("/foo", options.SubscriptionPath.ToString());
        }

        [Fact]
        public void SetQueryPath_Then_SetSubscriptionPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.QueryPath = new PathString("/foo");
            options.SubscriptionPath = new PathString("/bar");

            // act
            Assert.Equal("/foo/playground", options.Path.ToString());
            Assert.Equal("/foo", options.QueryPath.ToString());
            Assert.Equal("/bar", options.SubscriptionPath.ToString());
        }

        [Fact]
        public void SetSubscriptionPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.SubscriptionPath = new PathString("/foo");

            // act
            Assert.Equal("/playground", options.Path.ToString());
            Assert.Equal("/", options.QueryPath.ToString());
            Assert.Equal("/foo", options.SubscriptionPath.ToString());
        }

        [Fact]
        public void SetSubscriptionPath_Then_SetQueryPath()
        {
            // arrange
            var options = new PlaygroundOptions();

            // act
            options.SubscriptionPath = new PathString("/foo");
            options.QueryPath = new PathString("/bar");

            // act
            Assert.Equal("/bar/playground", options.Path.ToString());
            Assert.Equal("/bar", options.QueryPath.ToString());
            Assert.Equal("/foo", options.SubscriptionPath.ToString());
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
