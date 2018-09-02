using System;
using System.Reflection;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class FieldResolverTests
    {
        [Fact]
        public void Create()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            FieldResolverDelegate resolver = GetResolverA();

            // act
            var fieldMember = new FieldResolver(
                typeName, fieldName, resolver);

            // assert
            Assert.Equal(typeName, fieldMember.TypeName);
            Assert.Equal(fieldName, fieldMember.FieldName);
            Assert.Equal(resolver, fieldMember.Resolver);
        }

        [Fact]
        public void CreateTypeNull()
        {
            // arrange
            var fieldName = Guid.NewGuid().ToString();
            FieldResolverDelegate resolver = GetResolverA();

            // act
            Action action = () => new FieldResolver(null, fieldName, resolver);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CreateFieldNull()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            FieldResolverDelegate resolver = GetResolverA();

            // act
            Action action = () => new FieldResolver(typeName, null, resolver);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CreateMemberNull()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();

            // act
            Action action = () => new FieldResolver(typeName, fieldName, null);

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
            FieldResolverDelegate resolver = GetResolverA();

            var fieldMember = new FieldResolver(
                originalTypeName, fieldName, resolver);

            // act
            fieldMember = fieldMember.WithTypeName(newTypeName);

            // assert
            Assert.Equal(newTypeName, fieldMember.TypeName);
        }

        [Fact]
        public void WithTypeNameNull()
        {
            // arrange
            var originalTypeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            FieldResolverDelegate resolver = GetResolverA();

            var fieldMember = new FieldResolver(
                originalTypeName, fieldName, resolver);

            // act
            Action action = () => fieldMember.WithTypeName(null);

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
            FieldResolverDelegate resolver = GetResolverA();

            var fieldMember = new FieldResolver(
                typeName, originalFieldName, resolver);

            // act
            fieldMember = fieldMember.WithFieldName(newFieldName);

            // assert
            Assert.Equal(newFieldName, fieldMember.FieldName);
        }

        [Fact]
        public void WithFieldNameNull()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var originalFieldName = Guid.NewGuid().ToString();
            FieldResolverDelegate resolver = GetResolverA();

            var fieldMember = new FieldResolver(
                typeName, originalFieldName, resolver);

            // act
            Action action = () => fieldMember.WithFieldName(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void WithResolver()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            FieldResolverDelegate originalResolver = GetResolverA();
            FieldResolverDelegate newResolver = GetResolverB();

            var fieldMember = new FieldResolver(
                typeName, fieldName, originalResolver);

            // act
            fieldMember = fieldMember.WithResolver(newResolver);

            // assert
            Assert.Equal(newResolver, fieldMember.Resolver);
        }

        [Fact]
        public void WithResolverNull()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            FieldResolverDelegate originalResolver = GetResolverA();

            var fieldMember = new FieldResolver(
                typeName, fieldName, originalResolver);

            // act
            Action action = () => fieldMember.WithResolver(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void EqualsObjectNull()
        {
            // arrange
            var fieldMember = new FieldResolver(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetResolverA());

            // act
            bool result = fieldMember.Equals(default(object));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectReferenceEquals()
        {
            // arrange
            var fieldMember = new FieldResolver(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetResolverA());

            // act
            bool result = fieldMember.Equals((object)fieldMember);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsObjectFieldsAreEqual()
        {
            // arrange
            var fieldMember_a = new FieldResolver(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetResolverA());

            var fieldMember_b = new FieldResolver(
                fieldMember_a.TypeName,
                fieldMember_a.FieldName,
                fieldMember_a.Resolver);

            // act
            bool result = fieldMember_a.Equals((object)fieldMember_b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsObjectWithIncompatibleType()
        {
            // arrange
            var fieldMember = new FieldResolver(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetResolverA());

            // act
            bool result = fieldMember.Equals(new object());

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectTypeNotEqual()
        {
            // arrange
            var fieldMember_a = new FieldResolver(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetResolverA());

            var fieldMember_b = new FieldResolver(
                Guid.NewGuid().ToString(),
                fieldMember_a.FieldName,
                fieldMember_a.Resolver);

            // act
            bool result = fieldMember_a.Equals((object)fieldMember_b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectFieldNotEqual()
        {
            // arrange
            var fieldMember_a = new FieldResolver(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetResolverA());

            var fieldMember_b = new FieldResolver(
                fieldMember_a.TypeName,
                Guid.NewGuid().ToString(),
                fieldMember_a.Resolver);

            // act
            bool result = fieldMember_a.Equals((object)fieldMember_b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectMemberNotEqual()
        {
            // arrange
            var fieldMember_a = new FieldResolver(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetResolverA());

            var fieldMember_b = new FieldResolver(
                fieldMember_a.TypeName,
                fieldMember_a.FieldName,
                GetResolverB());

            // act
            bool result = fieldMember_a.Equals((object)fieldMember_b);

            // assert
            Assert.False(result);
        }

        private FieldResolverDelegate GetResolverA()
        {
            return new FieldResolverDelegate((a, b) => null);
        }

        private FieldResolverDelegate GetResolverB()
        {
            return new FieldResolverDelegate((a, b) => null);
        }

        private class Foo
        {
            public string BarA { get; }
            public string BarB { get; }
        }
    }
}
