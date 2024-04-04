using System;

namespace HotChocolate.Types.Relay;

public class DefaultNodeIdSerializerTests
{
    [Fact]
    public void Format_Small_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("Rm9vOmFiYw==", id);
    }

    [Fact]
    public void Parse_Small_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOmFiYw==");

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Format_480_Byte_Long_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

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
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

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
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        void Error() => serializer.Format("Baz", "abc");

        Assert.Throws<NodeIdMissingSerializerException>(Error);
    }

    [Fact]
    public void Serialize_Int16Id()
    {
        var serializer = CreateSerializer("Foo", new Int16NodeIdValueSerializer());

        var id = serializer.Format("Foo", (short)6);

        Assert.Equal("Rm9vOjY=", id);
    }

    [Fact]
    public void Serialize_Int32Id()
    {
        var serializer = CreateSerializer("Foo", new Int32NodeIdValueSerializer());

        var id = serializer.Format("Foo", 32);

        Assert.Equal("Rm9vOjMy", id);
    }

    [Fact]
    public void Serialize_Int64Id()
    {
        var serializer = CreateSerializer("Foo", new Int64NodeIdValueSerializer());

        var id = serializer.Format("Foo", (long)64);

        Assert.Equal("Rm9vOjY0", id);
    }

    [Fact]
    public void Serialize_Guid()
    {
        var serializer = CreateSerializer("Foo", new GuidNodeIdValueSerializer());

        var id = serializer.Format("Foo", Guid.Empty);

        Assert.Equal("Rm9vOgAAAAAAAAAAAAAAAAAAAAA=", id);
    }

    [Fact]
    public void Serialize_Guid_Normal()
    {
        var serializer = CreateSerializer("Foo", new GuidNodeIdValueSerializer(false));

        var id = serializer.Format("Foo", Guid.Empty);

        Assert.Equal("Rm9vOjAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAw", id);
    }

    [Fact]
    public void Format_CompositeId()
    {
        var serializer = CreateSerializer("Foo", new CompositeIdNodeIdValueSerializer());

        var id = serializer.Format("Foo", new CompositeId("foo", 42, Guid.Empty, true));

        Assert.Equal("Rm9vOmZvbzo0MjoAAAAAAAAAAAAAAAAAAAAAOjE=", id);
    }

    [Fact]
    public void Parse_CompositeId()
    {
        var compositeId = new CompositeId("foo", 42, Guid.Empty, true);
        var serializer = CreateSerializer("Foo", new CompositeIdNodeIdValueSerializer());
        var id = serializer.Format("Foo", compositeId);

        var parsed = serializer.Parse(id);

        Assert.Equal(compositeId, parsed.InternalId);
    }

    private static DefaultNodeIdSerializer CreateSerializer(string typeName, INodeIdValueSerializer serializer)
    {
        return new DefaultNodeIdSerializer(
            [new BoundNodeIdValueSerializer(typeName, serializer)],
            [serializer]);
    }

    private sealed class CompositeIdNodeIdValueSerializer : CompositeNodeIdValueSerialize<CompositeId>
    {
        protected override NodeIdFormatterResult Format(Span<byte> buffer, CompositeId value, out int written)
        {
            if (TryFormatIdPart(buffer, value.A, out var a) &&
                TryFormatIdPart(buffer.Slice(a), value.B, out var b) &&
                TryFormatIdPart(buffer.Slice(a + b), value.C, out var c) &&
                TryFormatIdPart(buffer.Slice(a + b + c), value.D, out var d))
            {
                written = a + b + c + d;
                return NodeIdFormatterResult.Success;
            }

            written = 0;
            return NodeIdFormatterResult.BufferTooSmall;
        }

        protected override bool TryParse(ReadOnlySpan<byte> buffer, out CompositeId value)
        {
            if(TryParseIdPart(buffer, out string a, out var ac) &&
                TryParseIdPart(buffer.Slice(ac), out int? b, out var bc) &&
                TryParseIdPart(buffer.Slice(ac + bc), out Guid? c, out var cc) &&
                TryParseIdPart(buffer.Slice(ac + bc + cc), out bool? d, out _))
            {
                value = new CompositeId(a, b.Value, c.Value, d.Value);
                return true;
            }

            value = default;
            return false;
        }
    }

    private readonly record struct CompositeId(string A, int B, Guid C, bool D);
}
