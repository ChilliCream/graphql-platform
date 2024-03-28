namespace HotChocolate.Types.Relay;

public class DefaultNodeIdSerializerTests
{
    [Fact]
    public void SerializeStringId()
    {
        var serializer = new DefaultNodeIdSerializer(
            [new NodeIdSerializerEntry("Foo", new StringNodeIdValueSerializer())]);

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("Foo:abc", id);
    }
}
