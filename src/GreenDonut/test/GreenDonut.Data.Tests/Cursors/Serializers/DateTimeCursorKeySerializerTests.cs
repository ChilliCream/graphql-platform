using System.Globalization;

namespace GreenDonut.Data.Cursors.Serializers;

public class DateTimeCursorKeySerializerTests
{
    private static readonly DateTimeCursorKeySerializer s_serializer = new();

    [Theory]
    [MemberData(nameof(Data))]
    public void Format(DateTime dateTime, byte[] result)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[23];

        // act
        var success = s_serializer.TryFormat(dateTime, buffer, out var written);

        // assert
        Assert.True(success);
        Assert.Equal(result, buffer);
        Assert.Equal(23, written);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void Parse(DateTime result, byte[] formattedKey)
    {
        // arrange & act
        var dateTime = (DateTime)s_serializer.Parse(formattedKey);

        // assert
        Assert.Equal(result, dateTime);
        Assert.Equal(result.Kind, dateTime.Kind);
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("sv-SE")]
    public void TryFormat_Should_ProduceInvariantBytes_When_CurrentCultureIsNonInvariant(string cultureName)
    {
        // arrange
        var dateTime = new DateTime(2026, 3, 5, 13, 45, 30, DateTimeKind.Utc).AddTicks(1234567);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Span<byte> invariantBuffer = stackalloc byte[23];
            s_serializer.TryFormat(dateTime, invariantBuffer, out _);
            var invariantBytes = invariantBuffer.ToArray();

            // act
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            Span<byte> buffer = stackalloc byte[23];
            var success = s_serializer.TryFormat(dateTime, buffer, out var written);
            var parsed = (DateTime)s_serializer.Parse(buffer);

            // assert
            Assert.True(success);
            Assert.Equal(23, written);
            Assert.Equal(invariantBytes, buffer.ToArray());
            Assert.Equal(dateTime, parsed);
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
        var dateTime = new DateTime(2026, 3, 5, 13, 45, 30, DateTimeKind.Utc).AddTicks(1234567);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
            Span<byte> buffer = stackalloc byte[23];
            s_serializer.TryFormat(dateTime, buffer, out _);
            var formatted = buffer.ToArray();

            // act
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var parsed = (DateTime)s_serializer.Parse(formatted);

            // assert
            Assert.Equal(dateTime, parsed);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    public static TheoryData<DateTime, byte[]> Data()
    {
        return new TheoryData<DateTime, byte[]>
        {
            // kind: unspecified
            {
                DateTime.MinValue,
                "000101010000000000000#0"u8.ToArray()
            },
            {
                DateTime.MaxValue,
                "999912312359599999999#0"u8.ToArray()
            },
            // kind: UTC
            {
                new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc),
                "000101010000000000000#1"u8.ToArray()
            },
            {
                DateTime.UnixEpoch,
                "197001010000000000000#1"u8.ToArray()
            },
            {
                new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc),
                "999912312359599999999#1"u8.ToArray()
            },
            // kind: local
            {
                new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Local),
                "000101010000000000000#2"u8.ToArray()
            },
            {
                new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Local),
                "999912312359599999999#2"u8.ToArray()
            }
        };
    }
}
