namespace Mocha.Tests;

public sealed class TransportHealthTests
{
    [Fact]
    public void Status_Should_BeWorstEntryStatus_When_EntriesDiffer()
    {
        // arrange
        TransportHealthEntry[] entries =
        [
            Entry("connection", TransportHealthStatus.Healthy),
            Entry("consumer:orders", TransportHealthStatus.Degraded),
            Entry("consumer:payments", TransportHealthStatus.Healthy)
        ];

        // act
        var health = new TransportHealth(entries);

        // assert
        Assert.Equal(TransportHealthStatus.Degraded, health.Status);
        Assert.Equal(entries, health.Entries);
    }

    [Fact]
    public void Healthy_Should_HaveNoEntries_When_TransportReportsNothing()
    {
        // act
        var health = TransportHealth.Healthy;

        // assert
        Assert.Equal(TransportHealthStatus.Healthy, health.Status);
        Assert.Empty(health.Entries);
    }

    private static TransportHealthEntry Entry(string name, TransportHealthStatus status)
        => new() { Name = name, Status = status, Description = status.ToString() };
}
