using System.Globalization;

namespace GreenDonut.Data.Cursors.Serializers;

public class DateTimeOffsetCursorKeySerializerTests
{
    private static readonly DateTimeOffsetCursorKeySerializer s_serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(DateTimeOffset dateTimeOffset, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[26];

        // act
        var success = s_serializer.TryFormat(dateTimeOffset, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(26, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(DateTimeOffset result, byte[] formattedKey)
    {
        // arrange & act
        var dateTimeOffset = (DateTimeOffset)s_serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, dateTimeOffset);
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("sv-SE")]
    public void TryFormat_Should_ProduceInvariantBytes_When_CurrentCultureIsNonInvariant(string cultureName)
    {
        // arrange
        var dateTimeOffset = new DateTimeOffset(2026, 3, 5, 13, 45, 30, TimeSpan.FromMinutes(90))
            .AddTicks(1234567);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Span<byte> invariantBuffer = stackalloc byte[26];
            s_serializer.TryFormat(dateTimeOffset, invariantBuffer, out _);
            var invariantBytes = invariantBuffer.ToArray();

            // act
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            Span<byte> buffer = stackalloc byte[26];
            var success = s_serializer.TryFormat(dateTimeOffset, buffer, out var written);
            var parsed = (DateTimeOffset)s_serializer.Parse(buffer);

            // assert
            Assert.True(success);
            Assert.Equal(26, written);
            Assert.Equal(invariantBytes, buffer.ToArray());
            Assert.Equal(dateTimeOffset, parsed);
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
        var dateTimeOffset = new DateTimeOffset(2026, 3, 5, 13, 45, 30, TimeSpan.FromMinutes(90))
            .AddTicks(1234567);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
            Span<byte> buffer = stackalloc byte[26];
            s_serializer.TryFormat(dateTimeOffset, buffer, out _);
            var formatted = buffer.ToArray();

            // act
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var parsed = (DateTimeOffset)s_serializer.Parse(formatted);

            // assert
            Assert.Equal(dateTimeOffset, parsed);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    public static TheoryData<DateTimeOffset, byte[]> Data()
    {
        return new TheoryData<DateTimeOffset, byte[]>
        {
            // negative offset
            {
                DateTimeOffset.UnixEpoch.ToOffset(TimeSpan.FromMinutes(-90)),
                "196912312230000000000-0130"u8.ToArray()
            },
            {
                DateTimeOffset.MaxValue.ToOffset(TimeSpan.FromMinutes(-90)),
                "999912312229599999999-0130"u8.ToArray()
            },
            // zero offset
            {
                DateTimeOffset.MinValue,
                "000101010000000000000+0000"u8.ToArray()
            },
            {
                DateTimeOffset.UnixEpoch,
                "197001010000000000000+0000"u8.ToArray()
            },
            {
                DateTimeOffset.MaxValue,
                "999912312359599999999+0000"u8.ToArray()
            },
            // positive offset
            {
                DateTimeOffset.MinValue.ToOffset(TimeSpan.FromMinutes(90)),
                "000101010130000000000+0130"u8.ToArray()
            },
            {
                DateTimeOffset.UnixEpoch.ToOffset(TimeSpan.FromMinutes(90)),
                "197001010130000000000+0130"u8.ToArray()
            }
        };
    }
}
