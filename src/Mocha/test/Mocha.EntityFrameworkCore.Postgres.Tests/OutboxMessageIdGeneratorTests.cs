using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class OutboxMessageIdGeneratorTests
{
    [Fact]
    public void Next_Should_ReturnStrictlyIncreasingIds_When_GeneratedWithinTheSameMillisecond()
    {
        // arrange
        const int count = 10_000;

        // act
        var ids = new string[count];
        for (var i = 0; i < count; i++)
        {
            ids[i] = Convert.ToHexString(OutboxMessageIdGenerator.Next().ToByteArray(bigEndian: true));
        }

        // assert
        // the first 12 hex digits are the millisecond timestamp
        Assert.Contains(ids.Zip(ids.Skip(1)), p => p.First[..12] == p.Second[..12]);
        Assert.Equal(ids.Order(StringComparer.Ordinal).Distinct(), ids);
    }

    [Fact]
    public void Next_Should_SetVersion7AndRfcVariant_When_IdIsGenerated()
    {
        // act
        var bytes = OutboxMessageIdGenerator.Next().ToByteArray(bigEndian: true);

        // assert
        Assert.Equal((0x70, 0x80), (bytes[6] & 0xF0, bytes[8] & 0xC0));
    }
}
