namespace HotChocolate.Types.Relay;

public class DefaultNodeIdSerializerTests
{
    [Fact]
    public void Serialize_Small_StringId()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new StringNodeIdValueSerializer())]);

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("Rm9vOmFiYw==", id);
    }

    [Fact]
    public void Serialize_480_Byte_Long_StringId()
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
}
