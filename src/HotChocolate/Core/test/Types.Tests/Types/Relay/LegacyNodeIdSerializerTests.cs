using Moq;

namespace HotChocolate.Types.Relay;

public class LegacyNodeIdSerializerTests
{
    [Fact]
    public void Format_Empty_StringId()
    {
        var serializer = CreateSerializer();

        var id = serializer.Format("Foo", "");

        Assert.Equal("Rm9vCmQ=", id);
    }

    [Fact]
    public void Format_Small_StringId()
    {
        var serializer = CreateSerializer();

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("Rm9vCmRhYmM=", id);
    }

    [Fact]
    public void Parse_Small_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

        var serializer = CreateSerializer();

        var id = serializer.Parse("Rm9vCmRhYmM=", lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Parse_Empty_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

        var serializer = CreateSerializer();

        var id = serializer.Parse("Rm9vCmQ=", lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("", id.InternalId);
    }

    [Fact]
    public void Parse_Empty_StringId2()
    {
        var serializer = CreateSerializer();

        var id = serializer.Parse("Rm9vCmQ=", typeof(string));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("", id.InternalId);
    }

    [Fact]
    public void Format_480_Byte_Long_StringId()
    {
        var serializer = CreateSerializer();

        var id = serializer.Format("Foo", new string('a', 480));

        Assert.Equal(
            "Rm9vCmRhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWE=",
            id);
    }

    [Fact]
    public void Parse_480_Byte_Long_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

        var serializer = CreateSerializer();

        var id = serializer.Parse(
            "Rm9vCmRhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWE=",
            lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal(new string('a', 480), id.InternalId);
    }

    [Fact]
    public void Serialize_Int16Id()
    {
        var serializer = CreateSerializer();

        var id = serializer.Format("Foo", (short)6);

        Assert.Equal("Rm9vCnM2", id);
    }

    [Fact]
    public void Serialize_Int32Id()
    {
        var serializer = CreateSerializer();

        var id = serializer.Format("Foo", 32);

        Assert.Equal("Rm9vCmkzMg==", id);
    }

    [Fact]
    public void Serialize_Int64Id()
    {
        var serializer = CreateSerializer();

        var id = serializer.Format("Foo", (long)64);

        Assert.Equal("Rm9vCmw2NA==", id);
    }

    [Fact]
    public void Serialize_Guid()
    {
        var serializer = CreateSerializer();

        var internalId = new Guid("1ae27b14-8cf6-440d-9a46-09090a4af6f3");
        var id = serializer.Format("Foo", internalId);

        Assert.Equal("Rm9vCmcxYWUyN2IxNDhjZjY0NDBkOWE0NjA5MDkwYTRhZjZmMw==", id);
    }

    [Fact]
    public void Serialize_Empty_Guid()
    {
        var serializer = CreateSerializer();

        var id = serializer.Format("Foo", Guid.Empty);

        Assert.Equal("Rm9vCmcwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMA==", id);
    }

    private static LegacyNodeIdSerializer CreateSerializer() => new();
}
