using System;
using Xunit;

namespace HotChocolate.Types
{
    public class MultiplierPathStringTests
    {
        [Fact]
        public void Create_DefaultConstructor_IsEmpty()
        {
            // arrange
            // act
            var nameString = new MultiplierPathString();

            // assert
            Assert.True(nameString.IsEmpty);
            Assert.False(nameString.HasValue);
            Assert.Null(nameString.Value);
        }

        [InlineData("_")]
        [InlineData("_Test")]
        [InlineData("_1Test")]
        [InlineData("Test")]
        [InlineData("Test_Test")]
        [InlineData("Test_Test.Test")]
        [Theory]
        public void Create_WithValidName_HasValue(string value)
        {
            // arrange
            // act
            var nameString = new MultiplierPathString(value);

            // assert
            Assert.Equal(nameString.Value, value);
            Assert.True(nameString.HasValue);
            Assert.False(nameString.IsEmpty);
        }

        [InlineData("1Test")]
        [InlineData("Test-Test")]
        [InlineData(".Test-Test")]
        [Theory]
        public void Create_WithInvalidName_ArgumentException(string value)
        {
            // arrange
            // act
            Action a = () => new MultiplierPathString(value);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [InlineData("_")]
        [InlineData("_Test")]
        [InlineData("_1Test")]
        [InlineData("Test")]
        [InlineData("Test_Test")]
        [InlineData("Test_Test.Test")]
        [Theory]
        public void ImplicitCast_ValidName_MultiplierPathString(string name)
        {
            // arrange
            // act
            MultiplierPathString nameString = name;

            // assert
            Assert.Equal(name, nameString.Value);
        }

        [InlineData("1Test")]
        [InlineData("Test-Test")]
        [InlineData("Täst")]
        [InlineData(".Test-Test")]
        [Theory]
        public void ImplicitCast_InvalidName_MultiplierPathString(string name)
        {
            // arrange
            // act
            Action a = () => { MultiplierPathString nameString = name; };

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void ImplicitCast_MultiplierPathString_String()
        {
            // arrange
            var nameString = new MultiplierPathString("Foo");

            // act
            string name = nameString;

            // assert
            Assert.Equal(nameString.Value, name);
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            // arrange
            var nameString = new MultiplierPathString("Foo");

            // act
            string value = nameString.ToString(); ;

            // assert
            Assert.Equal(nameString.Value, value);
        }

        [Fact]
        public void Append_MultiplierPathString_CombinedMultiplierPathString()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = new MultiplierPathString("Bar");

            // act
            MultiplierPathString combined = a.Add(b);

            // assert
            Assert.Equal("FooBar", combined.ToString());
        }

        [Fact]
        public void AddOp_MultiplierPathString_CombinedMultiplierPathString()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = new MultiplierPathString("Bar");

            // act
            MultiplierPathString combined = a + b;

            // assert
            Assert.Equal("FooBar", combined.ToString());
        }

        [Fact]
        public void AddOp_String_CombinedMultiplierPathString()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = "Bar";

            // act
            MultiplierPathString combined = a + b;

            // assert
            Assert.Equal("FooBar", combined.ToString());
        }

        [Fact]
        public void Equals_MultiplierPathStringWithSameValue_True()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = new MultiplierPathString("Foo");

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_SameInstance_True()
        {
            // arrange
            var a = new MultiplierPathString("Foo");

            // act
            bool result = a.Equals(a);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsIgnoreCasing_MultiplierPathStringWithDifferentCasing_True()
        {
            // arrange
            var a = new MultiplierPathString("FOO");
            var b = new MultiplierPathString("foo");

            // act
            bool result = a.Equals(b, StringComparison.OrdinalIgnoreCase);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_MultiplierPathStringDifferentValue_False()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = new MultiplierPathString("Bar");

            // act
            bool result = a.Equals(b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_StringWithSameValue_True()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = "Foo";

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_StringDifferentValue_False()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = "Bar";

            // act
            bool result = a.Equals(b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsOp_MultiplierPathStringWithSameValue_True()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = new MultiplierPathString("Foo");

            // act
            bool result = a == b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsOp_SameInstance_True()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            MultiplierPathString b = a;

            // act
            bool result = a == b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsOp_MultiplierPathStringDifferentValue_False()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = new MultiplierPathString("Bar");

            // act
            bool result = a == b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsOp_StringWithSameValue_True()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = "Foo";

            // act
            bool result = a == b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsOp_StringDifferentValue_False()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = "Bar";

            // act
            bool result = a == b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void NotEqualsOp_MultiplierPathStringWithSameValue_False()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = new MultiplierPathString("Foo");

            // act
            bool result = a != b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void NotEqualsOp_SameInstance_False()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            MultiplierPathString b = a;

            // act
            bool result = a != b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void NotEqualsOp_MultiplierPathStringDifferentValue_True()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = new MultiplierPathString("Bar");

            // act
            bool result = a != b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void NotEqualsOp_StringWithSameValue_False()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = "Foo";

            // act
            bool result = a != b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void NotEqualsOp_StringDifferentValue_True()
        {
            // arrange
            var a = new MultiplierPathString("Foo");
            var b = "Bar";

            // act
            bool result = a != b;

            // assert
            Assert.True(result);
        }
    }
}
