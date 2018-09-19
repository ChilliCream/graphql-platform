using System;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class FieldReferenceTests
    {
        [Fact]
        public void Create()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();

            // act
            var fieldReference = new FieldReference(typeName, fieldName);

            // assert
            Assert.Equal(typeName, fieldReference.TypeName);
            Assert.Equal(fieldName, fieldReference.FieldName);
        }

        [Fact]
        public void CreateTypeNull()
        {
            // arrange
            var fieldName = Guid.NewGuid().ToString();

            // act
            Action action = () => new FieldReference(null, fieldName);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CreateFieldNull()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();

            // act
            Action action = () => new FieldReference(typeName, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void WithTypeName()
        {
            // arrange
            var originalTypeName = Guid.NewGuid().ToString();
            var newTypeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            var fieldReference = new FieldReference(
                originalTypeName, fieldName);

            // act
            fieldReference = fieldReference.WithTypeName(newTypeName);

            // assert
            Assert.Equal(newTypeName, fieldReference.TypeName);
        }

        [Fact]
        public void WithTypeNameNull()
        {
            // arrange
            var originalTypeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            var fieldReference = new FieldReference(
                originalTypeName, fieldName);

            // act
            Action action = () => fieldReference.WithTypeName(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void WithFieldName()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var originalFieldName = Guid.NewGuid().ToString();
            var newFieldName = Guid.NewGuid().ToString();
            var fieldReference = new FieldReference(
                typeName, originalFieldName);

            // act
            fieldReference = fieldReference.WithFieldName(newFieldName);

            // assert
            Assert.Equal(newFieldName, fieldReference.FieldName);
        }

        [Fact]
        public void WithFieldNameNull()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var originalFieldName = Guid.NewGuid().ToString();
            var fieldReference = new FieldReference(
                typeName, originalFieldName);

            // act
            Action action = () => fieldReference.WithFieldName(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void EqualsObjectNull()
        {
            // arrange
            var fieldReference = new FieldReference(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            // act
            bool result = fieldReference.Equals(default(object));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectReferenceEquals()
        {
            // arrange
            var fieldReference = new FieldReference(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            // act
            bool result = fieldReference.Equals((object)fieldReference);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsObjectFieldsAreEqual()
        {
            // arrange
            var fieldReference_a = new FieldReference(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var fieldReference_b = new FieldReference(
                fieldReference_a.TypeName,
                fieldReference_a.FieldName);

            // act
            bool result = fieldReference_a.Equals((object)fieldReference_b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsObjectWithIncompatibleType()
        {
            // arrange
            var fieldReference = new FieldReference(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            // act
            bool result = fieldReference.Equals(new object());

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectTypeNotEqual()
        {
            // arrange
            var fieldReference_a = new FieldReference(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var fieldReference_b = new FieldReference(
                Guid.NewGuid().ToString(),
                fieldReference_a.FieldName);

            // act
            bool result = fieldReference_a.Equals((object)fieldReference_b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectFieldNotEqual()
        {
            // arrange
            var fieldReference_a = new FieldReference(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var fieldReference_b = new FieldReference(
                fieldReference_a.TypeName,
                Guid.NewGuid().ToString());

            // act
            bool result = fieldReference_a.Equals((object)fieldReference_b);

            // assert
            Assert.False(result);
        }
    }
}
