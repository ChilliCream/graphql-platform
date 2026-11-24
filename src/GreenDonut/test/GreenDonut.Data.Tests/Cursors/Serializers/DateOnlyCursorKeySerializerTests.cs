using System.Globalization;

namespace GreenDonut.Data.Cursors.Serializers;

public class DateOnlyCursorKeySerializerTests
{
    private static readonly DateOnlyCursorKeySerializer s_serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(DateOnly dateOnly, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[8];

        // act
        var success = s_serializer.TryFormat(dateOnly, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(8, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(DateOnly result, byte[] formattedKey)
    {
        // arrange & act
        var dateOnly = (DateOnly)s_serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, dateOnly);
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("sv-SE")]
    public void TryFormat_Should_ProduceInvariantBytes_When_CurrentCultureIsNonInvariant(string cultureName)
    {
        // arrange
        var dateOnly = new DateOnly(2026, 3, 5);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Span<byte> invariantBuffer = stackalloc byte[8];
            s_serializer.TryFormat(dateOnly, invariantBuffer, out _);
            var invariantBytes = invariantBuffer.ToArray();

            // act
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            Span<byte> buffer = stackalloc byte[8];
            var success = s_serializer.TryFormat(dateOnly, buffer, out var written);
            var parsed = (DateOnly)s_serializer.Parse(buffer);

            // assert
            Assert.True(success);
            Assert.Equal(8, written);
            Assert.Equal(invariantBytes, buffer.ToArray());
            Assert.Equal(dateOnly, parsed);
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
        var dateOnly = new DateOnly(2026, 3, 5);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
            Span<byte> buffer = stackalloc byte[8];
            s_serializer.TryFormat(dateOnly, buffer, out _);
            var formatted = buffer.ToArray();

            // act
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var parsed = (DateOnly)s_serializer.Parse(formatted);

            // assert
            Assert.Equal(dateOnly, parsed);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    public static TheoryData<DateOnly, byte[]> Data()
    {
        return new TheoryData<DateOnly, byte[]>
        {
            {
                DateOnly.MinValue,
                "00010101"u8.ToArray()
            },
            {
                DateOnly.FromDateTime(DateTime.UnixEpoch),
                "19700101"u8.ToArray()
            },
            {
                DateOnly.MaxValue,
                "99991231"u8.ToArray()
            }
        };
    }
}
