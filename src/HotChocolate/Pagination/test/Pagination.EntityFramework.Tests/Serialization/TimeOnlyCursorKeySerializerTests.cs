using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Data.Serialization;

public class TimeOnlyCursorKeySerializerTests
{
    private static readonly TimeOnlyCursorKeySerializer Serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(TimeOnly timeOnly, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[13];

        // act
        var success = Serializer.TryFormat(timeOnly, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(13, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(TimeOnly result, byte[] formattedKey)
    {
        // arrange & act
        var timeOnly = (TimeOnly)Serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, timeOnly);
    }

    public static TheoryData<TimeOnly, byte[]> Data()
    {
        return new TheoryData<TimeOnly, byte[]>
        {
            {
                TimeOnly.MinValue,
                "0000000000000"u8.ToArray()
            },
            {
                TimeOnly.MaxValue,
                "2359599999999"u8.ToArray()
            }
        };
    }
}
