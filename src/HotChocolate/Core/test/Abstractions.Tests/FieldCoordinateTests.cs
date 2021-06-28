using Xunit;

namespace HotChocolate
{
    public class FieldCoordinateTests
    {
        [Fact]
        public void ToString_Field()
        {
            // arrange
            var fieldCoordinate = new FieldCoordinate("abc", "def");

            // act
            // assert
            Assert.Equal("abc.def", fieldCoordinate.ToString());
        }

        [Fact]
        public void GetHashCode_Field()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def");
            var b = new FieldCoordinate("abc", "def");

            // act
            // assert
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void ToString_Field_Argument()
        {
            // arrange
            var fieldCoordinate = new FieldCoordinate("abc", "def", "ghi");

            // act
            // assert
            Assert.Equal("abc.def(ghi)", fieldCoordinate.ToString());
        }

        [Fact]
        public void GetHashCode_Field_Argument()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def", "ghi");
            var b = new FieldCoordinate("abc", "def", "ghi");

            // act
            // assert
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void HasValue_False()
        {
            // arrange
            var fieldCoordinate = new FieldCoordinate();

            // act
            // assert
            Assert.False(fieldCoordinate.HasValue);
        }

        [Fact]
        public void HasValue_True()
        {
            // arrange
            var fieldCoordinate = new FieldCoordinate("abc", "def");

            // act
            // assert
            Assert.True(fieldCoordinate.HasValue);
        }

        [Fact]
        public void With_TypeName()
        {
            // arrange
            var fieldCoordinate = new FieldCoordinate("abc", "def");

            // act
            fieldCoordinate = fieldCoordinate.With(typeName: new NameString("xyz"));

            // assert
            Assert.Equal("xyz.def", fieldCoordinate.ToString());
        }

        [Fact]
        public void With_FieldName()
        {
            // arrange
            var fieldCoordinate = new FieldCoordinate("abc", "def");

            // act
            fieldCoordinate = fieldCoordinate.With(fieldName: new NameString("xyz"));

            // assert
            Assert.Equal("abc.xyz", fieldCoordinate.ToString());
        }

        [Fact]
        public void With_Argument()
        {
            // arrange
            var fieldCoordinate = new FieldCoordinate("abc", "def");

            // act
            fieldCoordinate = fieldCoordinate.With(argumentName: new NameString("xyz"));

            // assert
            Assert.Equal("abc.def(xyz)", fieldCoordinate.ToString());
        }

        [Fact]
        public void CreateWithoutTypeName()
        {
            // arrange
            // act
            var fieldCoordinate = FieldCoordinate.CreateWithoutType("abc");

            // assert
            Assert.Equal("__Empty.abc", fieldCoordinate.ToString());
        }

        [Fact]
        public void Equals_Field_True()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def");
            var b = new FieldCoordinate("abc", "def");

            // act
            // assert
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_Field_False()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def");
            var b = new FieldCoordinate("abc", "xyz");

            // act
            // assert
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_Field_To_Arg_False()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def");
            var b = new FieldCoordinate("abc", "def", "ghi");

            // act
            // assert
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_Argument_True()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def", "ghi");
            var b = new FieldCoordinate("abc", "def", "ghi");

            // act
            // assert
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_Argument_False()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def", "ghi");
            var b = new FieldCoordinate("abc", "def", "xyz");

            // act
            // assert
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Object_Equals_Field_To_Arg_False()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def");
            var b = new FieldCoordinate("abc", "def", "ghi");

            // act
            // assert
            Assert.False(a.Equals((object)b));
        }

        [Fact]
        public void Object_Equals_Argument_True()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def", "ghi");
            var b = new FieldCoordinate("abc", "def", "ghi");

            // act
            // assert
            Assert.True(a.Equals((object)b));
        }

        [Fact]
        public void Object_Equals_Argument_False()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def", "ghi");
            var b = new FieldCoordinate("abc", "def", "xyz");

            // act
            // assert
            Assert.False(a.Equals((object)b));
        }

        [Fact]
        public void Deconstruct_Coordinates()
        {
            // arrange
            var a = new FieldCoordinate("abc", "def", "ghi");
            var b = new FieldCoordinate("abc", "def");

            // act
            (NameString at, NameString af, NameString? aa) = a;
            (NameString bt, NameString bf, NameString? ba) = b;

            // assert
            Assert.Equal(a.TypeName, at);
            Assert.Equal(a.FieldName, af);
            Assert.Equal(a.ArgumentName, aa);
            Assert.Equal(b.TypeName, bt);
            Assert.Equal(b.FieldName, bf);
            Assert.Equal(b.ArgumentName, ba);
        }
    }
}
