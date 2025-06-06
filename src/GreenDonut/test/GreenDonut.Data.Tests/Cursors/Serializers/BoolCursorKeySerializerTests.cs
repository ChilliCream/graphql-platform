namespace GreenDonut.Data.Cursors.Serializers;

public class BoolCursorKeySerializerTests
{
    private static readonly BoolCursorKeySerializer s_serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(bool boolean, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[1];

        // act
        var success = s_serializer.TryFormat(boolean, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(1, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(bool result, byte[] formattedKey)
    {
        // arrange & act
        var boolean = (bool)s_serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, boolean);
    }

    public static TheoryData<bool, byte[]> Data()
    {
        return new TheoryData<bool, byte[]>
        {
            {
                false,
                "0"u8.ToArray()
            },
            {
                true,
                "1"u8.ToArray()
            }
        };
    }
}
