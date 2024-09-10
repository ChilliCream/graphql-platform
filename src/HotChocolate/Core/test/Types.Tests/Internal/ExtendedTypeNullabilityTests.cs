#nullable enable

namespace HotChocolate.Internal;

public class ExtendedTypeNullabilityTests
{
    private readonly TypeCache _typeCache = new TypeCache();

    [InlineData("Array", "[Byte!]!")]
    [InlineData("NullableArray", "[Byte!]")]
    [InlineData("ArrayNullableElement", "[Byte]!")]
    [InlineData("NullableArrayNullableElement", "[Byte]")]
    [InlineData("ObjectArray", "[Object!]!")]
    [InlineData("NullableObjectArray", "[Object!]")]
    [InlineData("ObjectArrayNullableElement", "[Object]!")]
    [InlineData("NullableObjectArrayNullableElement", "[Object]")]
    [Theory]
    public void DetectNullabilityOnArrays(string methodName, string typeName)
    {
        // arrange
        var method = typeof(Arrays).GetMethod(methodName)!;

        // act
        var extendedType = ExtendedType.FromMember(method, _typeCache);

        // assert
        Assert.Equal(typeName, extendedType.ToString());
    }

    [InlineData("List", "List<Byte!>!")]
    [InlineData("NullableList", "List<Byte!>")]
    [InlineData("ListNullableElement", "List<Byte>!")]
    [InlineData("NullableListNullableElement", "List<Byte>")]
    [InlineData("ObjectList", "List<Object!>!")]
    [InlineData("NullableObjectList", "List<Object!>")]
    [InlineData("ObjectListNullableElement", "List<Object>!")]
    [InlineData("NullableObjectListNullableElement", "List<Object>")]
    [Theory]
    public void DetectNullabilityOnLists(string methodName, string typeName)
    {
        // arrange
        var method = typeof(Lists).GetMethod(methodName)!;

        // act
        var extendedType = ExtendedType.FromMember(method, _typeCache);

        // assert
        Assert.Equal(typeName, extendedType.ToString());
    }

    [InlineData("Dict1", "Dictionary<Byte!, Object>!")]
    [InlineData("Dict2", "Dictionary<Byte!, Object>")]
    [InlineData(
        "Tuple",
        "Tuple<Tuple<Int32!, Int32>!, Tuple<Object!, Tuple<Object, String!>!>!>")]
    [InlineData("TaskAsyncEnumerable", "IAsyncEnumerable<String!>!")]
    [InlineData("ValueTaskAsyncEnumerable", "IAsyncEnumerable<String!>!")]
    [Theory]
    public void DetectNullabilityWithGenerics(string methodName, string typeName)
    {
        // arrange
        var method = typeof(Generics).GetMethod(methodName)!;

        // act
        var extendedType = ExtendedType.FromMember(method, _typeCache);

        // assert
        Assert.Equal(typeName, extendedType.ToString());
    }

    public class Arrays
    {
        public byte[] Array()
        {
            throw new NotImplementedException();
        }

        public byte[]? NullableArray()
        {
            throw new NotImplementedException();
        }

        public byte?[] ArrayNullableElement()
        {
            throw new NotImplementedException();
        }

        public byte?[]? NullableArrayNullableElement()
        {
            throw new NotImplementedException();
        }

        public object[] ObjectArray()
        {
            throw new NotImplementedException();
        }

        public object[]? NullableObjectArray()
        {
            throw new NotImplementedException();
        }

        public object?[] ObjectArrayNullableElement()
        {
            throw new NotImplementedException();
        }

        public object?[]? NullableObjectArrayNullableElement()
        {
            throw new NotImplementedException();
        }
    }

    public class Lists
    {
        public List<byte> List()
        {
            throw new NotImplementedException();
        }

        public List<byte>? NullableList()
        {
            throw new NotImplementedException();
        }

        public List<byte?> ListNullableElement()
        {
            throw new NotImplementedException();
        }

        public List<byte?>? NullableListNullableElement()
        {
            throw new NotImplementedException();
        }

        public List<object> ObjectList()
        {
            throw new NotImplementedException();
        }

        public List<object>? NullableObjectList()
        {
            throw new NotImplementedException();
        }

        public List<object?> ObjectListNullableElement()
        {
            throw new NotImplementedException();
        }

        public List<object?>? NullableObjectListNullableElement()
        {
            throw new NotImplementedException();
        }
    }

    public class Generics
    {
        public Dictionary<byte, object?> Dict1() =>
            throw new NotImplementedException();

        public Dictionary<byte, object?>? Dict2() =>
            throw new NotImplementedException();

        public Tuple<Tuple<int, int?>, Tuple<object, Tuple<object?, string>>>? Tuple() =>
            throw new NotImplementedException();

        public Task<IAsyncEnumerable<string>> TaskAsyncEnumerable() =>
            throw new NotImplementedException();

        public ValueTask<IAsyncEnumerable<string>> ValueTaskAsyncEnumerable() =>
            throw new NotImplementedException();
    }
}
