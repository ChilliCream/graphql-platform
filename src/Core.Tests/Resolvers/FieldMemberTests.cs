using System;
using System.Reflection;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class FieldMemberTests
    {
        [Fact]
        public void Create()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            MemberInfo member = GetMemberA();

            // act
            var fieldMember = new FieldMember(
                typeName, fieldName, member);

            // assert
            Assert.Equal(typeName, fieldMember.TypeName);
            Assert.Equal(fieldName, fieldMember.FieldName);
            Assert.Equal(member, fieldMember.Member);
        }

        [Fact]
        public void CreateTypeNull()
        {
            // arrange
            var fieldName = Guid.NewGuid().ToString();
            MemberInfo member = GetMemberA();

            // act
            Action action = () => new FieldMember(null, fieldName, member);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CreateFieldNull()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            MemberInfo member = GetMemberA();

            // act
            Action action = () => new FieldMember(typeName, null, member);

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
            Action action = () => new FieldMember(typeName, fieldName, null);

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
            MemberInfo member = GetMemberA();

            var fieldMember = new FieldMember(
                originalTypeName, fieldName, member);

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
            MemberInfo member = GetMemberA();

            var fieldMember = new FieldMember(
                originalTypeName, fieldName, member);

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
            MemberInfo member = GetMemberA();

            var fieldMember = new FieldMember(
                typeName, originalFieldName, member);

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
            MemberInfo member = GetMemberA();

            var fieldMember = new FieldMember(
                typeName, originalFieldName, member);

            // act
            Action action = () => fieldMember.WithFieldName(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void WithMember()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            MemberInfo originalMember = GetMemberA();
            MemberInfo newMember = GetMemberB();

            var fieldMember = new FieldMember(
                typeName, fieldName, originalMember);

            // act
            fieldMember = fieldMember.WithMember(newMember);

            // assert
            Assert.Equal(newMember, fieldMember.Member);
        }

        [Fact]
        public void WithMemberNull()
        {
            // arrange
            var typeName = Guid.NewGuid().ToString();
            var fieldName = Guid.NewGuid().ToString();
            MemberInfo originalMember = GetMemberA();

            var fieldMember = new FieldMember(
                typeName, fieldName, originalMember);

            // act
            Action action = () => fieldMember.WithMember(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void EqualsObjectNull()
        {
            // arrange
            var fieldMember = new FieldMember(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetMemberA());

            // act
            bool result = fieldMember.Equals(default(object));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectReferenceEquals()
        {
            // arrange
            var fieldMember = new FieldMember(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetMemberA());

            // act
            bool result = fieldMember.Equals((object)fieldMember);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsObjectFieldsAreEqual()
        {
            // arrange
            var fieldMember_a = new FieldMember(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetMemberA());

            var fieldMember_b = new FieldMember(
                fieldMember_a.TypeName,
                fieldMember_a.FieldName,
                fieldMember_a.Member);

            // act
            bool result = fieldMember_a.Equals((object)fieldMember_b);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void EqualsObjectWithIncompatibleType()
        {
            // arrange
            var fieldMember = new FieldMember(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetMemberA());

            // act
            bool result = fieldMember.Equals(new object());

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectTypeNotEqual()
        {
            // arrange
            var fieldMember_a = new FieldMember(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetMemberA());

            var fieldMember_b = new FieldMember(
                Guid.NewGuid().ToString(),
                fieldMember_a.FieldName,
                fieldMember_a.Member);

            // act
            bool result = fieldMember_a.Equals((object)fieldMember_b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectFieldNotEqual()
        {
            // arrange
            var fieldMember_a = new FieldMember(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetMemberA());

            var fieldMember_b = new FieldMember(
                fieldMember_a.TypeName,
                Guid.NewGuid().ToString(),
                fieldMember_a.Member);

            // act
            bool result = fieldMember_a.Equals((object)fieldMember_b);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void EqualsObjectMemberNotEqual()
        {
            // arrange
            var fieldMember_a = new FieldMember(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                GetMemberA());

            var fieldMember_b = new FieldMember(
                fieldMember_a.TypeName,
                fieldMember_a.FieldName,
                GetMemberB());

            // act
            bool result = fieldMember_a.Equals((object)fieldMember_b);

            // assert
            Assert.False(result);
        }

        private MemberInfo GetMemberA()
        {
            return typeof(Foo).GetProperty("BarA");
        }

        private MemberInfo GetMemberB()
        {
            return typeof(Foo).GetProperty("BarB");
        }

        private class Foo
        {
            public string BarA { get; }
            public string BarB { get; }
        }
    }
}
