using HotChocolate.Internal;
using HotChocolate.Types;

namespace HotChocolate.Utilities;

public class BaseTypesTests
{
    [InlineData(typeof(StringType), true)]
    [InlineData(typeof(ScalarType), false)]
    [InlineData(typeof(ListType<StringType>), true)]
    [InlineData(typeof(NonNullType<StringType>), true)]
    [InlineData(typeof(InputObjectType), false)]
    [InlineData(typeof(InputObjectType<Foo>), true)]
    [InlineData(typeof(ObjectType), false)]
    [InlineData(typeof(ObjectType<Foo>), true)]
    [InlineData(typeof(EnumType), false)]
    [InlineData(typeof(EnumType<FooEnum>), true)]
    [InlineData(typeof(InterfaceType), false)]
    [InlineData(typeof(InterfaceType<object>), true)]
    [InlineData(typeof(UnionType), false)]
    [InlineData(typeof(UnionType<object>), true)]
    [InlineData(typeof(Foo), false)]
    [InlineData(typeof(FooEnum), false)]
    [Theory]
    public void IsSchemaType(Type type, bool expectedResult)
    {
        // act
        var result = ExtendedType.Tools.IsSchemaType(type);

        // assert
        Assert.Equal(expectedResult, result);
    }

    [InlineData(typeof(StringType), false)]
    [InlineData(typeof(ScalarType), true)]
    [InlineData(typeof(ListType<StringType>), false)]
    [InlineData(typeof(NonNullType<StringType>), false)]
    [InlineData(typeof(InputObjectType), true)]
    [InlineData(typeof(InputObjectType<Foo>), false)]
    [InlineData(typeof(ObjectType), true)]
    [InlineData(typeof(ObjectType<Foo>), false)]
    [InlineData(typeof(EnumType), true)]
    [InlineData(typeof(EnumType<FooEnum>), false)]
    [InlineData(typeof(InterfaceType), true)]
    [InlineData(typeof(UnionType), true)]
    [InlineData(typeof(Foo), false)]
    [InlineData(typeof(FooEnum), false)]
    [Theory]
    public void IsNonGenericBaseType(Type type, bool expectedResult)
    {
        // act
        var result = ExtendedType.Tools.IsNonGenericBaseType(type);

        // assert
        Assert.Equal(expectedResult, result);
    }

    public class Foo
    {
        public string Bar { get; set; }
    }

    public enum FooEnum
    {
        Bar,
    }
}
