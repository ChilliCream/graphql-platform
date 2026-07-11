using System.Text;

namespace GreenDonut.Data.Cursors.Serializers;

public class EnumCursorKeySerializerTests
{
    private static readonly EnumCursorKeySerializer<int> s_serializer = new();

    [Fact]
    public void IsSupported_NonNullable_Enum()
    {
        Assert.True(s_serializer.IsSupported(typeof(TestIntEnum)));
    }

    [Fact]
    public void IsSupported_Nullable_Enum()
    {
        Assert.True(s_serializer.IsSupported(typeof(TestIntEnum?)));
    }

    [Fact]
    public void IsSupported_Different_Underlying_Type()
    {
        Assert.False(s_serializer.IsSupported(typeof(TestByteEnum)));
    }

    [Fact]
    public void Registration_Finds_Nullable_Enum_Serializer()
    {
        var serializer = CursorKeySerializerRegistration.Find(typeof(TestIntEnum?));
        Assert.IsType<EnumCursorKeySerializer<int>>(serializer);
    }

    [Theory]
    [InlineData(TestIntEnum.One, "1")]
    [InlineData(TestIntEnum.Two, "2")]
    public void TryFormat(TestIntEnum value, string expected)
    {
        Span<byte> buffer = stackalloc byte[16];

        var success = s_serializer.TryFormat(value, buffer, out var written);

        Assert.True(success);
        Assert.Equal(expected, Encoding.UTF8.GetString(buffer[..written]));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    public void Parse(string formatted, int expected)
    {
        var result = s_serializer.Parse(Encoding.UTF8.GetBytes(formatted));
        Assert.Equal(expected, Assert.IsType<int>(result));
    }

    public enum TestIntEnum
    {
        One = 1,
        Two = 2
    }

    public enum TestByteEnum : byte
    {
        One = 1
    }
}
