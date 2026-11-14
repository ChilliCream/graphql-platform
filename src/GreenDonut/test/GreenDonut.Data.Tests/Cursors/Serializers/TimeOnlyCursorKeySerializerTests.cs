using System.Globalization;

namespace GreenDonut.Data.Cursors.Serializers;

public class TimeOnlyCursorKeySerializerTests
{
    private static readonly TimeOnlyCursorKeySerializer s_serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(TimeOnly timeOnly, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[13];

        // act
        var success = s_serializer.TryFormat(timeOnly, buffer, out var written);

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
        var timeOnly = (TimeOnly)s_serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, timeOnly);
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("sv-SE")]
    public void TryFormat_Should_ProduceInvariantBytes_When_CurrentCultureIsNonInvariant(string cultureName)
    {
        // arrange
        var timeOnly = new TimeOnly(13, 45, 30).Add(TimeSpan.FromTicks(1234567));
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Span<byte> invariantBuffer = stackalloc byte[13];
            s_serializer.TryFormat(timeOnly, invariantBuffer, out _);
            var invariantBytes = invariantBuffer.ToArray();

            // act
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            Span<byte> buffer = stackalloc byte[13];
            var success = s_serializer.TryFormat(timeOnly, buffer, out var written);
            var parsed = (TimeOnly)s_serializer.Parse(buffer);

            // assert
            Assert.True(success);
            Assert.Equal(13, written);
            Assert.Equal(invariantBytes, buffer.ToArray());
            Assert.Equal(timeOnly, parsed);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Parse_Should_DecodeCorrectly_When_FormattedUnderThTh_AndParsedUnderInvariant()
    {
        // arrange
        var timeOnly = new TimeOnly(13, 45, 30).Add(TimeSpan.FromTicks(1234567));
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
            Span<byte> buffer = stackalloc byte[13];
            s_serializer.TryFormat(timeOnly, buffer, out _);
            var formatted = buffer.ToArray();

            // act
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var parsed = (TimeOnly)s_serializer.Parse(formatted);

            // assert
            Assert.Equal(timeOnly, parsed);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
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
