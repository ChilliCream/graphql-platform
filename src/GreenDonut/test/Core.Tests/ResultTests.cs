using System;
using System.Collections.Generic;
using Xunit;

namespace GreenDonut
{
    public class ResultTests
    {
        [Fact(DisplayName = "Equals: Should return false if comparing error with value")]
        public void EqualsErrorValue()
        {
            // arrange
            Result<string> error = new Exception("Foo");
            Result<string> value = "Bar";

            // act
            bool result = error.Equals(value);

            // assert
            Assert.False(result);
        }

        [Fact(DisplayName = "Equals: Should return false if the error is not equal")]
        public void EqualsDifferentError()
        {
            // arrange
            Result<string> errorA = new Exception("Foo");
            Result<string> errorB = new Exception("Bar");

            // act
            bool result = errorA.Equals(errorB);

            // assert
            Assert.False(result);
        }

        [Fact(DisplayName = "Equals: Should return true if the error is equal")]
        public void EqualsSameError()
        {
            // arrange
            Result<string> error = new Exception("Foo");

            // act
            bool result = error.Equals(error);

            // assert
            Assert.True(result);
        }

        [Fact(DisplayName = "Equals: Should return false if the error is not equal")]
        public void EqualsDifferentValue()
        {
            // arrange
            Result<string> valueA = "Foo";
            Result<string> valueB = "Bar";

            // act
            bool result = valueA.Equals(valueB);

            // assert
            Assert.False(result);
        }

        [Fact(DisplayName = "Equals: Should return true if the value is equal")]
        public void EqualsSameValue()
        {
            // arrange
            Result<string> value = "Foo";

            // act
            bool result = value.Equals(value);

            // assert
            Assert.True(result);
        }

        [Fact(DisplayName = "Equals: Should return false if object is null")]
        public void EqualsOpjectNull()
        {
            // arrange
            object obj = null;
            Result<string> value = "Foo";

            // act
            bool result = value.Equals(obj);

            // assert
            Assert.False(result);
        }

        [Fact(DisplayName = "Equals: Should return false if object type is different")]
        public void EqualsOpjectNotEqual()
        {
            // arrange
            object obj = "Foo";
            Result<string> value = "Bar";

            // act
            bool result = value.Equals(obj);

            // assert
            Assert.False(result);
        }

        [Fact(DisplayName = "Equals: Should return false if object value is different")]
        public void EqualsOpjectValueNotEqual()
        {
            // arrange
            object obj = (Result<string>)"Foo";
            Result<string> value = "Bar";

            // act
            bool result = value.Equals(obj);

            // assert
            Assert.False(result);
        }

        [Fact(DisplayName = "Equals: Should return true if object value is equal")]
        public void EqualsOpjectValueEqual()
        {
            // arrange
            object obj = (Result<string>)"Foo";
            Result<string> value = "Foo";

            // act
            bool result = value.Equals(obj);

            // assert
            Assert.True(result);
        }

        [Fact(DisplayName = "GetHashCode: Should return 0")]
        public void GetHashCodeEmpty()
        {
            // arrange
            string value = null;

            // act
            Result<string> result = value;

            // assert
            Assert.Equal(0, result.GetHashCode());
        }

        [Fact(DisplayName = "GetHashCode: Should return a hash code for value")]
        public void GetHashCodeValue()
        {
            // arrange
            var value = "Foo";

            // act
            Result<string> result = value;

            // assert
            Assert.Equal(value.GetHashCode(), result.GetHashCode());
        }

        [Fact(DisplayName = "GetHashCode: Should return a hash code for error")]
        public void GetHashCodeError()
        {
            // arrange
            var error = new Exception();

            // act
            Result<string> result = error;

            // assert
            Assert.Equal(error.GetHashCode(), result.GetHashCode());
        }

        [Fact(DisplayName = "ImplicitReject: Should return a resolved Result if error is null")]
        public void ImplicitRejectErrorIsNull()
        {
            // arrange
            Exception error = null;

            // act
            Result<object> result = error;

            // assert
            Assert.False(result.IsError);
            Assert.Null(result.Error);
            Assert.Null(result.Value);
        }

        [Fact(DisplayName = "ImplicitReject: Should return a rejected Result")]
        public void ImplicitReject()
        {
            // arrange
            var errorMessage = "Foo";
            var error = new Exception(errorMessage);

            // act
            Result<string> result = error;

            // assert
            Assert.True(result.IsError);
            Assert.Equal(error, result.Error);
            Assert.Null(result.Value);
        }

        [Fact(DisplayName = "ExplicitReject: Should return a rejected Result")]
        public void ExplicitReject()
        {
            // arrange
            var errorMessage = "Foo";
            var error = new Exception(errorMessage);

            // act
            var result = Result<string>.Reject(error);

            // assert
            Assert.True(result.IsError);
            Assert.Equal("Foo", result.Error?.Message);
            Assert.Null(result.Value);
        }

        [InlineData(null)]
        [InlineData("Foo")]
        [Theory(DisplayName = "ImplicitResolve: Should return a resolved Result")]
        public void ImplicitResolve(string value)
        {
            // act
            Result<string> result = value;

            // assert
            Assert.Null(result.Error);
            Assert.False(result.IsError);
            Assert.Equal(value, result);
        }

        [InlineData(null)]
        [InlineData("Foo")]
        [Theory(DisplayName = "ExplicitResolve: Should return a resolved Result")]
        public void ExplicitResolve(string value)
        {
            // act
            var result = Result<string>.Resolve(value);

            // assert
            Assert.Null(result.Error);
            Assert.False(result.IsError);
            Assert.Equal(value, result.Value);
        }

        [Fact(DisplayName = "ExplicitResolve: Should return a resolved Result of list")]
        public void ExplicitResolveList()
        {
            // arrange
            var value = new[] { "Foo", "Bar", "Baz" };

            // act
            var result = Result<IReadOnlyCollection<string>>.Resolve(value);

            // assert
            Assert.Null(result.Error);
            Assert.False(result.IsError);
            Assert.Equal(value, result.Value);
        }
    }
}
