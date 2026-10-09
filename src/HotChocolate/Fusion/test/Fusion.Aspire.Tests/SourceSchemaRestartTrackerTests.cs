namespace HotChocolate.Fusion.Aspire;

public sealed class SourceSchemaRestartTrackerTests
{
    [Fact]
    public void IsRestart_Should_ReturnFalse_When_ResourceSignalsReadyForTheFirstTime()
    {
        // arrange
        var tracker = new SourceSchemaRestartTracker();

        // act
        var isRestart = tracker.IsRestart("products");

        // assert
        Assert.False(isRestart);
    }

    [Fact]
    public void IsRestart_Should_ReturnTrue_When_ResourceSignalsReadyAgain()
    {
        // arrange
        var tracker = new SourceSchemaRestartTracker();
        tracker.IsRestart("products");

        // act
        var isRestart = tracker.IsRestart("products");

        // assert
        Assert.True(isRestart);
    }

    [Fact]
    public void IsRestart_Should_TrackResourcesIndependently_When_AnotherResourceAlreadyRestarted()
    {
        // arrange
        var tracker = new SourceSchemaRestartTracker();
        tracker.IsRestart("products");
        tracker.IsRestart("products");

        // act
        var isRestart = tracker.IsRestart("orders");

        // assert
        Assert.False(isRestart);
    }
}
