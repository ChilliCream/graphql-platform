using Moq;

namespace HotChocolate.Types;

[Obsolete("Use IsStructurallyEqual(this IType x, IType y) instead.")]
public class TypeExtensionsTests
{
    [Fact]
    public void IsEquals_TwoStringNonNullTypes_True()
    {
        // arrange
        var x = new NonNullType(new StringType());
        var y = new NonNullType(new StringType());

        // act
        var result = x.IsEqualTo(y);

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
        var result = x.IsEqualTo(y);

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
        var result = x.IsEqualTo(y);

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
        var result = x.IsEqualTo(y);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsEquals_StringToSelf_True()
    {
        // arrange
        var x = new StringType();

        // act
        var result = x.IsEqualTo(x);

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
        var result = x.IsEqualTo(y);

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
        var stringType = type.NamedType() as StringType;

        // assert
        Assert.NotNull(stringType);
    }

    [Fact]
    public static void NamedType_Of_T()
    {
        // arrange
        var type = new NonNullType(
            new ListType(
                new NonNullType(
                    new StringType())));

        // act
        var stringType = type.NamedType<StringType>();

        // assert
        Assert.NotNull(stringType);
    }

    [Fact]
    public static void NamedType_Of_T_Is_Not_Of_T()
    {
        // arrange
        var type = new NonNullType(
            new ListType(
                new NonNullType(
                    new StringType())));

        // act & assert
        Assert.Throws<ArgumentException>(type.NamedType<ObjectType>);
    }

    [Fact]
    public static void NamedType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.NamedType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsNonNullType_True()
    {
        // arrange
        var type = new NonNullType(new StringType());

        // act
        var result = type.IsNonNullType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsNonNullType_False()
    {
        // arrange
        var type = new StringType();

        // act
        var result = type.IsNonNullType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsNonNullType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsNonNullType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsCompositeType_ObjectType_True()
    {
        // arrange
        IType type = Mock.Of<ObjectType>(t => t.Kind == TypeKind.Object);

        // act
        var result = type.IsCompositeType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsCompositeType_InterfaceType_True()
    {
        // arrange
        IType type = Mock.Of<InterfaceType>(t => t.Kind == TypeKind.Interface);

        // act
        var result = type.IsCompositeType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsCompositeType_UnionType_True()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsCompositeType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsCompositeType_False()
    {
        // arrange
        var type = new StringType();

        // act
        var result = type.IsCompositeType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsCompositeType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsCompositeType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsComplexType_ObjectType_True()
    {
        // arrange
        IType type = Mock.Of<ObjectType>(t => t.Kind == TypeKind.Object);

        // act
        var result = type.IsComplexType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsComplexType_InterfaceType_True()
    {
        // arrange
        IType type = Mock.Of<InterfaceType>(t => t.Kind == TypeKind.Interface);

        // act
        var result = type.IsComplexType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsComplexType_UnionType_False()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsComplexType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsComplexType_False()
    {
        // arrange
        var type = new StringType();

        // act
        var result = type.IsComplexType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsComplexType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsComplexType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsLeafType_ScalarType_True()
    {
        // arrange
        var type = new StringType();

        // act
        var result = type.IsLeafType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsLeafType_EnumType_True()
    {
        // arrange
        IType type = Mock.Of<EnumType>(t => t.Kind == TypeKind.Enum);

        // act
        var result = type.IsLeafType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsLeafType_UnionType_False()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsLeafType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsLeafType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsLeafType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsListType_StringListType_True()
    {
        // arrange
        IType type = new ListType(new StringType());

        // act
        var result = type.IsListType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsListType_UnionType_False()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsListType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsListType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsListType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsScalarType_StringType_True()
    {
        // arrange
        IType type = new StringType();

        // act
        var result = type.IsScalarType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsScalarType_UnionType_False()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsScalarType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsScalarType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsScalarType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsObjectType_True()
    {
        // arrange
        IType type = Mock.Of<ObjectType>(t => t.Kind == TypeKind.Object);

        // act
        var result = type.IsObjectType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsObjectType_False()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsObjectType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsObjectType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsObjectType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsInterfaceType_True()
    {
        // arrange
        IType type = Mock.Of<InterfaceType>(t => t.Kind == TypeKind.Interface);

        // act
        var result = type.IsInterfaceType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsInterfaceType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsInterfaceType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsEnumType_True()
    {
        // arrange
        IType type = Mock.Of<EnumType>(t => t.Kind == TypeKind.Enum);

        // act
        var result = type.IsEnumType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsEnumType_False()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsEnumType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsEnumType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsEnumType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsUnionType_True()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsUnionType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsUnionType_False()
    {
        // arrange
        IType type = Mock.Of<ObjectType>(t => t.Kind == TypeKind.Object);

        // act
        var result = type.IsUnionType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsUnionType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsUnionType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsInputObjectType_True()
    {
        // arrange
        IType type = Mock.Of<InputObjectType>(t => t.Kind == TypeKind.InputObject);

        // act
        var result = type.IsInputObjectType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsInputObjectType_False()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsInputObjectType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsInputObjectType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsInputObjectType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsInputType_True()
    {
        // arrange
        IType type = new StringType();

        // act
        var result = type.IsInputType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsInputType_False()
    {
        // arrange
        IType type = Mock.Of<UnionType>();

        // act
        var result = type.IsInputType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsInputType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsInputType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsOutputType_True()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsOutputType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsOutputType_False()
    {
        // arrange
        IType type = Mock.Of<InputObjectType>(t => t.Kind == TypeKind.InputObject);

        // act
        var result = type.IsOutputType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsOutputType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsOutputType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsAbstractType_InterfaceType_True()
    {
        // arrange
        IType type = Mock.Of<InterfaceType>(t => t.Kind == TypeKind.Interface);

        // act
        var result = type.IsAbstractType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsAbstractType_UnionType_True()
    {
        // arrange
        IType type = Mock.Of<UnionType>(t => t.Kind == TypeKind.Union);

        // act
        var result = type.IsAbstractType();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsAbstractType_False()
    {
        // arrange
        IType type = Mock.Of<InputObjectType>(t => t.Kind == TypeKind.InputObject);

        // act
        var result = type.IsAbstractType();

        // assert
        Assert.False(result);
    }

    [Fact]
    public static void IsAbstractType_Type_Is_Null()
    {
        // act
        void Action() => HotChocolateTypesAbstractionsTypeExtensions.IsAbstractType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public static void IsType_StringType_True()
    {
        // arrange
        IType type = new StringType();

        // act
        var result = type.IsType(TypeKind.Scalar);

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsType_NonNullStringType_True()
    {
        // arrange
        IType type = new NonNullType(new StringType());

        // act
        var result = type.IsType(TypeKind.Scalar);

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsType_InputObjectType_False()
    {
        // arrange
        IType type = Mock.Of<InputObjectType>();

        // act
        var result = type.IsType(TypeKind.Scalar);

        // assert
        Assert.False(result);
    }
}
