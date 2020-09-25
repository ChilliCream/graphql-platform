using System;
using Moq;
using Xunit;

namespace HotChocolate.Types
{
    public class TypeExtensionsTests
    {
        [Fact]
        public void IsEquals_TwoStringNonNullTypes_True()
        {
            // arrange
            var x = new NonNullType(new StringType());
            var y = new NonNullType(new StringType());

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEquals_TwoStringListTypes_True()
        {
            // arrange
            var x = new ListType(new StringType());
            var y = new ListType(new StringType());

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEquals_TwoStringNonNullListTypes_True()
        {
            // arrange
            var x = new NonNullType(new ListType(new StringType()));
            var y = new NonNullType(new ListType(new StringType()));

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEquals_NonNullStringListToStringList_False()
        {
            // arrange
            var x = new NonNullType(new ListType(new StringType()));
            var y = new ListType(new StringType());

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEquals_StringToSelf_True()
        {
            // arrange
            var x = new StringType();

            // act
            bool result = x.IsEqualTo(x);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEquals_StringListToIntList_False()
        {
            // arrange
            var x = new ListType(new StringType());
            var y = new ListType(new IntType());

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void NamedType()
        {
            // arrange
            var type = new NonNullType(
                new ListType(
                    new NonNullType(
                        new StringType())));

            // act
            StringType stringType = type.NamedType() as StringType;

            // assert
            Assert.NotNull(stringType);
        }

        [Fact]
        public static void NamedType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.NamedType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsNonNullType_True()
        {
            // arrange
            var type = new NonNullType(new StringType());

            // act
            bool result = type.IsNonNullType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsNonNullType_False()
        {
            // arrange
            var type = new StringType();

            // act
            bool result = type.IsNonNullType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsNonNullType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsNonNullType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsCompositeType_ObjectType_True()
        {
            // arrange
            IType type = Mock.Of<ObjectType>();

            // act
            bool result = type.IsCompositeType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsCompositeType_InterfaceType_True()
        {
            // arrange
            IType type = Mock.Of<InterfaceType>();

            // act
            bool result = type.IsCompositeType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsCompositeType_UnionType_True()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsCompositeType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsCompositeType_False()
        {
            // arrange
            var type = new StringType();

            // act
            bool result = type.IsCompositeType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsCompositeType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsCompositeType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsComplexType_ObjectType_True()
        {
            // arrange
            IType type = Mock.Of<ObjectType>();

            // act
            bool result = type.IsComplexType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsComplexType_InterfaceType_True()
        {
            // arrange
            IType type = Mock.Of<InterfaceType>();

            // act
            bool result = type.IsComplexType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsComplexType_UnionType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsComplexType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsComplexType_False()
        {
            // arrange
            var type = new StringType();

            // act
            bool result = type.IsComplexType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsComplexType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsComplexType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsLeafType_ScalarType_True()
        {
            // arrange
            var type = new StringType();

            // act
            bool result = type.IsLeafType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsLeafType_EnumType_True()
        {
            // arrange
            IType type = Mock.Of<EnumType>();

            // act
            bool result = type.IsLeafType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsLeafType_UnionType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsLeafType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsLeafType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsLeafType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsListType_StringListType_True()
        {
            // arrange
            IType type = new ListType(new StringType());

            // act
            bool result = type.IsListType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsListType_UnionType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsListType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsListType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsListType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsScalarType_StringType_True()
        {
            // arrange
            IType type = new StringType();

            // act
            bool result = type.IsScalarType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsScalarType_UnionType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsScalarType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsScalarType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsScalarType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsObjectType_True()
        {
            // arrange
            IType type = Mock.Of<ObjectType>();

            // act
            bool result = type.IsObjectType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsObjectType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsObjectType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsObjectType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsObjectType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsInterfaceType_True()
        {
            // arrange
            IType type = Mock.Of<InterfaceType>();

            // act
            bool result = type.IsInterfaceType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsScalarType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsInterfaceType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsInterfaceType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsInterfaceType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsEnumType_True()
        {
            // arrange
            IType type = Mock.Of<EnumType>();

            // act
            bool result = type.IsEnumType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsEnumType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsEnumType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsEnumType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsEnumType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsUnionType_True()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsUnionType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsUnionType_False()
        {
            // arrange
            IType type = Mock.Of<ObjectType>();

            // act
            bool result = type.IsUnionType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsUnionType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsUnionType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsInputObjectType_True()
        {
            // arrange
            IType type = Mock.Of<InputObjectType>();

            // act
            bool result = type.IsInputObjectType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsInputObjectType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsInputObjectType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsInputObjectType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsInputObjectType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsInputType_True()
        {
            // arrange
            IType type = new StringType();

            // act
            bool result = type.IsInputType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsInputType_False()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsInputType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsInputType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsInputType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsOutputType_True()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsOutputType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsOutputType_False()
        {
            // arrange
            IType type = Mock.Of<InputObjectType>();

            // act
            bool result = type.IsOutputType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsOutputType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsOutputType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsAbstractType_InterfaceType_True()
        {
            // arrange
            IType type = Mock.Of<InterfaceType>();

            // act
            bool result = type.IsAbstractType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsAbstractType_UnionType_True()
        {
            // arrange
            IType type = Mock.Of<UnionType>();

            // act
            bool result = type.IsAbstractType();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsAbstractType_False()
        {
            // arrange
            IType type = Mock.Of<InputObjectType>();

            // act
            bool result = type.IsAbstractType();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsAbstractType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsAbstractType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public static void IsType_StringType_True()
        {
            // arrange
            IType type = new StringType();

            // act
            bool result = type.IsType<StringType>();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsType_NonNullStringType_True()
        {
            // arrange
            IType type = new NonNullType(new StringType());

            // act
            bool result = type.IsType<StringType>();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsType_InputObjectType_False()
        {
            // arrange
            IType type = Mock.Of<InputObjectType>();

            // act
            bool result = type.IsType<StringType>();

            // assert
            Assert.False(result);
        }

        [Fact]
        public static void IsType_Type_Is_Null()
        {
            // act
            Action action = () => TypeExtensions.IsAbstractType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
