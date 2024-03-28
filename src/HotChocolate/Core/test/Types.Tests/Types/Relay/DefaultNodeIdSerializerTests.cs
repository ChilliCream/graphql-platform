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
    public void Serialize_1024_Byte_Long_StringId()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new StringNodeIdValueSerializer())]);

        var id = serializer.Format("Foo", new string('a', 1024));

        Assert.Equal("Rm9vOmE=", id);
    }
}
