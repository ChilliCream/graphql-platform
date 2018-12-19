using System;
using HotChocolate;
using Xunit;

namespace HotChocolate
{
    public class NameStringTests
    {
        [Fact]
        public void Create_DefaultConstructor_IsEmpty()
        {
            // arrange
            // act
            var nameString = new NameString();

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
        [Theory]
        public void Create_WithValidName_HasValue(string value)
        {
            // arrange
            // act
            var nameString = new NameString(value);

            // assert
            Assert.Equal(nameString.Value, value);
            Assert.True(nameString.HasValue);
            Assert.False(nameString.IsEmpty);
        }

        [InlineData("1Test")]
        [InlineData("Test-Test")]
        [Theory]
        public void Create_WithInvalidName_ArgumentException(string value)
        {
            // arrange
            // act
            Action a = () => new NameString(value);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [InlineData("_")]
        [InlineData("_Test")]
        [InlineData("_1Test")]
        [InlineData("Test")]
        [InlineData("Test_Test")]
        [Theory]
        public void ImplicitCast_ValidName_NameString(string name)
        {
            // arrange
            // act
            NameString nameString = name;

            // assert
            Assert.Equal(name, nameString.Value);
        }

        [InlineData("1Test")]
        [InlineData("Test-Test")]
        [InlineData("Täst")]
        [Theory]
        public void ImplicitCast_InvalidName_NameString(string name)
        {
            // arrange
            // act
            Action a = () => { NameString nameString = name; };

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void ImplicitCast_NameString_String()
        {
            // arrange
            var nameString = new NameString("Foo");

            // act
            string name = nameString;

            // assert
            Assert.Equal(nameString.Value, name);
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            // arrange
            var nameString = new NameString("Foo");

            // act
            string value = nameString.ToString(); ;

            // assert
            Assert.Equal(nameString.Value, value);
        }

        [Fact]
        public void Append_NameString_CombinedNameString()
        {
            // arrange
            var a = new NameString("Foo");
            var b = new NameString("Bar");

            // act
            NameString combined = a.Add(b);

            // assert
            Assert.Equal("FooBar", combined.ToString());
        }

        [Fact]
        public void AddOp_NameString_CombinedNameString()
        {
            // arrange
            var a = new NameString("Foo");
            var b = new NameString("Bar");

            // act
            NameString combined = a + b;

            // assert
            Assert.Equal("FooBar", combined.ToString());
        }

        [Fact]
        public void AddOp_String_CombinedNameString()
        {
            // arrange
            var a = new NameString("Foo");
            var b = "Bar";

            // act
            NameString combined = a + b;

            // assert
            Assert.Equal("FooBar", combined.ToString());
        }

        [Fact]
        public void Equals_NameStringWithSameValue_True()
        {
            // arrange
            var a = new NameString("Foo");
            var b = new NameString("Foo");

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_SameInstance_True()
        {
            // arrange
            var a = new NameString("Foo");

            // act
            bool result = a.Equals(a);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsIgnoreCasing_NameStringWithDifferentCasing_True()
        {
            // arrange
            var a = new NameString("FOO");
            var b = new NameString("foo");

            // act
            bool result = a.Equals(b, StringComparison.OrdinalIgnoreCase);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_NameStringDifferentValue_False()
        {
            // arrange
            var a = new NameString("Foo");
            var b = new NameString("Bar");

            // act
            bool result = a.Equals(b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_StringWithSameValue_True()
        {
            // arrange
            var a = new NameString("Foo");
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
            var a = new NameString("Foo");
            var b = "Bar";

            // act
            bool result = a.Equals(b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsOp_NameStringWithSameValue_True()
        {
            // arrange
            var a = new NameString("Foo");
            var b = new NameString("Foo");

            // act
            bool result = a == b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsOp_SameInstance_True()
        {
            // arrange
            var a = new NameString("Foo");
            var b = a;

            // act
            bool result = a == b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsOp_NameStringDifferentValue_False()
        {
            // arrange
            var a = new NameString("Foo");
            var b = new NameString("Bar");

            // act
            bool result = a == b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsOp_StringWithSameValue_True()
        {
            // arrange
            var a = new NameString("Foo");
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
            var a = new NameString("Foo");
            var b = "Bar";

            // act
            bool result = a == b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void NotEqualsOp_NameStringWithSameValue_False()
        {
            // arrange
            var a = new NameString("Foo");
            var b = new NameString("Foo");

            // act
            bool result = a != b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void NotEqualsOp_SameInstance_False()
        {
            // arrange
            var a = new NameString("Foo");
            var b = a;

            // act
            bool result = a != b;

            // assert
            Assert.False(result);
        }

        [Fact]
        public void NotEqualsOp_NameStringDifferentValue_True()
        {
            // arrange
            var a = new NameString("Foo");
            var b = new NameString("Bar");

            // act
            bool result = a != b;

            // assert
            Assert.True(result);
        }

        [Fact]
        public void NotEqualsOp_StringWithSameValue_False()
        {
            // arrange
            var a = new NameString("Foo");
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
            var a = new NameString("Foo");
            var b = "Bar";

            // act
            bool result = a != b;

            // assert
            Assert.True(result);
        }
    }
}
