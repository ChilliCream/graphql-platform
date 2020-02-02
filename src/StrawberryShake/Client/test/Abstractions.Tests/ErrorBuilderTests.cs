using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StrawberryShake
{
    public class ErrorBuilderTests
    {
        [Fact]
        public void FromError()
        {
            // arrange
            IError error = new Error("123", null, null, null, null);

            // act
            var builder = ErrorBuilder.FromError(error);
            error = builder.Build();

            // assert
            Assert.Equal("123", error.Message);
        }

        [Fact]
        public void FromError_WithExtensions()
        {
            // arrange
            IError error = new Error(
                "123",
                null,
                null,
                new Dictionary<string, object>
                {
                    {"foo", "bar"}
                },
                null);

            // act
            var builder = ErrorBuilder.FromError(error);
            error = builder.Build();

            // assert
            Assert.Equal("123", error.Message);
            Assert.Collection(error.Extensions,
                t => Assert.Equal("bar", t.Value));
        }

        [Fact]
        public void FromError_ClearExtensions()
        {
            // arrange
            IError error = new Error(
                "123",
                null,
                null,
                new Dictionary<string, object>
                {
                    {"foo", "bar"}
                },
                null);

            // act
            error = ErrorBuilder.FromError(error).ClearExtensions().Build();

            // assert
            Assert.Equal("123", error.Message);
            Assert.Null(error.Extensions);
        }

        [Fact]
        public void FromError_RemoveExtension()
        {
            // arrange
            IError error = new Error(
                "123",
                null,
                null,
                new Dictionary<string, object>
                {
                    {"foo", "bar"},
                    {"bar", "foo"}
                },
                null);

            // act
            error = ErrorBuilder.FromError(error)
                .RemoveExtension("bar")
                .Build();

            // assert
            Assert.Equal("123", error.Message);
            Assert.Collection(error.Extensions,
                t => Assert.Equal("bar", t.Value));
        }

        [Fact]
        public void FromError_WithLocations()
        {
            // arrange
            IError error = new Error(
                "123",
                null,
                new List<Location> { new Location(1, 2) },
                null,
                null);

            // act
            var builder = ErrorBuilder.FromError(error);
            error = builder.Build();

            // assert
            Assert.Equal("123", error.Message);
            Assert.Collection(error.Locations,
                t => Assert.Equal(1, t.Line));
        }

        [Fact]
        public void FromError_ClearLocations()
        {
            // arrange
            IError error = new Error(
                "123",
                null,
                new List<Location> { new Location(1, 2) },
                null,
                null);

            // act
            error = ErrorBuilder.FromError(error).ClearLocations().Build();

            // assert
            Assert.Equal("123", error.Message);
            Assert.Null(error.Locations);
        }

        [Fact]
        public void FromError_ErrorNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => ErrorBuilder.FromError(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void SetMessage_MessageNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => ErrorBuilder.New().SetMessage(null);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SetMessage_Bar_ErrorCodeIsFoo()
        {
            // arrange
            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .Build();

            // assert
            Assert.Equal("bar", error.Message);
        }

        [Fact]
        public void SetCode_Foo_ErrorCodeIsFoo()
        {
            // arrange
            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .SetCode("foo")
                .Build();

            // assert
            Assert.Equal("foo", error.Code);
            Assert.Collection(error.Extensions,
                t => Assert.Equal("foo", t.Value));
        }

        [Fact]
        public void SetPath_Foo_PathIsFooWithCount1()
        {
            // arrange
            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .SetPath(new List<object> { "foo" })
                .Build();

            // assert
            Assert.Collection(error.Path,
                t => Assert.Equal("foo", t.ToString()));
        }

        [Fact]
        public void SetPathObject_Foo_PathIsFooWithCount1()
        {
            // arrange
            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .SetPath(new List<object> { "foo" })
                .Build();

            // assert
            Assert.Collection(error.Path,
                t => Assert.Equal("foo", t.ToString()));
        }

        [Fact]
        public void AddLocation()
        {
            // arrange
            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .AddLocation(new Location(2, 3))
                .Build();

            // assert
            Assert.Collection(error.Locations,
                t => Assert.Equal(2, t.Line));
        }

        [Fact]
        public void AddLocation2()
        {
            // arrange
            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .AddLocation(new Location(2, 3))
                .AddLocation(new Location(4, 5))
                .Build();

            // assert
            Assert.Collection(error.Locations,
                t => Assert.Equal(2, t.Line),
                t => Assert.Equal(4, t.Line));
        }

        [Fact]
        public void AddLocation3()
        {
            // arrange
            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .AddLocation(2, 3)
                .AddLocation(new Location(4, 5))
                .Build();

            // assert
            Assert.Collection(error.Locations,
                t => Assert.Equal(2, t.Line),
                t => Assert.Equal(4, t.Line));
        }

        [Fact]
        public void AddLocation_LineSmallerThan1_ArgumentException()
        {
            // arrange
            // act
            Action action = () => ErrorBuilder.New()
                  .AddLocation(0, 3);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void AddLocation_ColumnSmallerThan1_ArgumentException()
        {
            // arrange
            // act
            Action action = () => ErrorBuilder.New()
                  .AddLocation(2, 0);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void SetException()
        {
            // arrange
            var exception = new Exception();

            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .SetException(exception)
                .Build();

            // assert
            Assert.Equal(exception, error.Exception);
        }

        [Fact]
        public void SetExtension()
        {
            // arrange
            // act
            IError error = ErrorBuilder.New()
                .SetMessage("bar")
                .SetExtension("a", "b")
                .SetExtension("a", "c")
                .SetExtension("c", "d")
                .Build();

            // assert
            Assert.Collection(error.Extensions.OrderBy(t => t.Key),
                t => Assert.Equal("c", t.Value),
                t => Assert.Equal("d", t.Value));
        }

        [Fact]
        public void Build_NoMessage_InvalidOperationException()
        {
            // arrange
            // act
            Action action = () => ErrorBuilder.New().Build();

            // assert
            Assert.Throws<InvalidOperationException>(action);
        }
    }
}
