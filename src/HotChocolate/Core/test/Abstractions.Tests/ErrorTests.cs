using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HotChocolate
{
    public class ErrorTests
    {
        [Fact]
        public void WithCode()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            error = error.WithCode("foo");

            // assert
            Assert.Equal("foo", error.Code);
        }

        [Fact]
        public void WithException()
        {
            // arrange
            IError error = new Error
            {
                Message = "123",
                Exception = new Exception()
            };

            Assert.NotNull(error.Exception);

            // act
            error = error.RemoveException();

            // assert
            Assert.Null(error.Exception);
        }

        [Fact]
        public void WithExtensions()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            error = error.WithExtensions(
                new Dictionary<string, object> { { "a", "b" } });

            // assert
            Assert.Collection(error.Extensions,
                t =>
                {
                    Assert.Equal("a", t.Key);
                    Assert.Equal("b", t.Value);
                });
        }

        [Fact]
        public void AddExtensions()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            error = error.AddExtension("a", "b").AddExtension("c", "d");

            // assert
            Assert.Collection(error.Extensions.OrderBy(t => t.Key),
                t =>
                {
                    Assert.Equal("a", t.Key);
                    Assert.Equal("b", t.Value);
                },
                t =>
                {
                    Assert.Equal("c", t.Key);
                    Assert.Equal("d", t.Value);
                });
        }

        [Fact]
        public void RemoveExtensions()
        {
            // arrange
            IError error = new Error { Message = "123" };
            error = error.WithExtensions(
                new Dictionary<string, object>
                {
                    { "a", "b" },
                    { "c", "d" }
                });

            // act
            error = error.RemoveExtension("a");

            // assert
            Assert.Collection(error.Extensions,
                t =>
                {
                    Assert.Equal("c", t.Key);
                    Assert.Equal("d", t.Value);
                });
        }

        [Fact]
        public void WithLocations()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            error = error.WithLocations(
                new List<Location> { new Location(1, 2) });

            // assert
            Assert.Collection(error.Locations,
                t =>
                {
                    Assert.Equal(1, t.Line);
                    Assert.Equal(2, t.Column);
                });
        }

        [Fact]
        public void WithMessage()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            error = error.WithMessage("456");

            // assert
            Assert.Equal("456", error.Message);
        }

        [Fact]
        public void WithMessage_MessageNull_ArgumentException()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            Action action = () => error.WithMessage(null);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void WithMessage_MessageEmpty_ArgumentException()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            Action action = () => error.WithMessage(string.Empty);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void WithPath()
        {
            // arrange
            IError error = new Error { Message = "123" };

            // act
            error = error.WithPath(Path.New("foo"));

            // assert
            Assert.Collection(error.Path,
                t => Assert.Equal("foo", t.ToString()));
        }
    }
}
