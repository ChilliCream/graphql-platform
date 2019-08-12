using System;
using Xunit;
using HotChocolate.AspNetCore.Voyager;

namespace HotChocolate.AspNetCore
{
    public class VoyagerOptionsTests
    {
        [Fact]
        public void Default_Values()
        {
            // arrange
            // act
            var options = new VoyagerOptions();

            // act
            Assert.Equal("/voyager", options.Path);
            Assert.Equal("/", options.QueryPath);
        }

        [Fact]
        public void SetPath()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.Path = "/foo";

            // act
            Assert.Equal("/foo", options.Path);
            Assert.Equal("/", options.QueryPath);
        }

        [Fact]
        public void SetPath_Then_SetQueryPath()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.Path = "/foo";
            options.QueryPath = "/bar";

            // act
            Assert.Equal("/foo", options.Path);
            Assert.Equal("/bar", options.QueryPath);
        }

        [Fact]
        public void SetQueryPath()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.QueryPath = "/foo";

            // act
            Assert.Equal("/foo/voyager", options.Path);
            Assert.Equal("/foo", options.QueryPath);
        }

        [Fact]
        public void SetQueryPath_Then_SetPath()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.QueryPath = "/foo";
            options.Path = "/bar";

            // act
            Assert.Equal("/bar", options.Path);
            Assert.Equal("/foo", options.QueryPath);
        }

        [Fact]
        public void Path_Set_To_Empty_ArgumentException()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            Action action = () => options.Path = default;

            // act
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void QueryPath_Set_To_Empty_ArgumentException()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            Action action = () => options.QueryPath = default;

            // act
            Assert.Throws<ArgumentException>(action);
        }
    }
}
