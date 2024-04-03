using System;

namespace HotChocolate.Types.Relay;

public class DefaultNodeIdSerializerTests
{
    [Fact]
    public void Format_Small_StringId()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new StringNodeIdValueSerializer())]);

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("Rm9vOmFiYw==", id);
    }

    [Fact]
    public void Parse_Small_StringId()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new StringNodeIdValueSerializer())]);

        var id = serializer.Parse("Rm9vOmFiYw==");

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Format_480_Byte_Long_StringId()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new StringNodeIdValueSerializer())]);

        var id = serializer.Format("Foo", new string('a', 480));

        Assert.Equal(
            "Rm9vOmFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYQ==",
            id);
    }

    [Fact]
    public void Parse_480_Byte_Long_StringId()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new StringNodeIdValueSerializer())]);

        var id = serializer.Parse(
            "Rm9vOmFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYQ==");

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal(new string('a', 480), id.InternalId);
    }

    [Fact]
    public void Serialize_TypeName_Not_Registered()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new StringNodeIdValueSerializer())]);

        void Error() => serializer.Format("Baz", "abc");

        Assert.Throws<NodeIdMissingSerializerException>(Error);
    }

    [Fact]
    public void Serialize_Int16Id()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new Int16NodeIdValueSerializer())]);

        var id = serializer.Format("Foo", (short)6);

        Assert.Equal("Rm9vOjY=", id);
    }

    [Fact]
    public void Serialize_Int32Id()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new Int32NodeIdValueSerializer())]);

        var id = serializer.Format("Foo", 32);

        Assert.Equal("Rm9vOjMy", id);
    }

    [Fact]
    public void Serialize_Int64Id()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new Int64NodeIdValueSerializer())]);

        var id = serializer.Format("Foo", (long)64);

        Assert.Equal("Rm9vOjY0", id);
    }

    [Fact]
    public void Serialize_Guid()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new GuidNodeIdValueSerializer())]);

        var id = serializer.Format("Foo", Guid.Empty);

        Assert.Equal("Rm9vOgAAAAAAAAAAAAAAAAAAAAA=", id);
    }

    [Fact]
    public void Serialize_Guid_Normal()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new GuidNodeIdValueSerializer(false))]);

        var id = serializer.Format("Foo", Guid.Empty);

        Assert.Equal("Rm9vOjAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAw", id);
    }
}
