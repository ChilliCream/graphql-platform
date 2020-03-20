using System;
using Xunit;
using HotChocolate.AspNetClassic.Voyager;
using Microsoft.Owin;

namespace HotChocolate.AspNetClassic
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
            Assert.Equal("/voyager", options.Path.ToString());
            Assert.Equal("/", options.QueryPath.ToString());
            Assert.Null(options.GraphQLEndpoint);
        }

        [Fact]
        public void SetPath()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.Path = new PathString("/foo");

            // act
            Assert.Equal("/foo", options.Path.ToString());
            Assert.Equal("/", options.QueryPath.ToString());
        }

        [Fact]
        public void SetGraphQLEndpoint()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.GraphQLEndpoint = new Uri("https://localhost:8081/graphql");

            // act
            Assert.Equal("https://localhost:8081/graphql", options.GraphQLEndpoint.AbsoluteUri);
        }

        [Fact]
        public void SetPath_Then_SetQueryPath()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.Path = new PathString("/foo");
            options.QueryPath = new PathString("/bar");

            // act
            Assert.Equal("/foo", options.Path.ToString());
            Assert.Equal("/bar", options.QueryPath.ToString());
        }

        [Fact]
        public void SetQueryPath()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.QueryPath = new PathString("/foo");

            // act
            Assert.Equal("/foo/voyager", options.Path.ToString());
            Assert.Equal("/foo", options.QueryPath.ToString());
        }

        [Fact]
        public void SetQueryPath_Then_SetPath()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            options.QueryPath = new PathString("/foo");
            options.Path = new PathString("/bar");

            // act
            Assert.Equal("/bar", options.Path.ToString());
            Assert.Equal("/foo", options.QueryPath.ToString());
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

        [Fact]
        public void GraphQLEndpoint_Set_To_Null_Or_Invalid_Exceptions()
        {
            // arrange
            var options = new VoyagerOptions();

            // act
            Action invalidAction = () => options.GraphQLEndpoint = new Uri("not valid");
            Action nullAction = () => options.GraphQLEndpoint = new Uri(null);

            // act
            Assert.Throws<UriFormatException>(invalidAction);
            Assert.Throws<ArgumentNullException>(nullAction);
        }
    }
}
