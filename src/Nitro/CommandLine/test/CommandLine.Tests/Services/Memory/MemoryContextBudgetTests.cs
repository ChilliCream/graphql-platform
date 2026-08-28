using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// The context budget determinism contract test: given the same ranked
/// candidates, limit, and character budget, <see cref="MemoryContextBudget.Select"/>
/// always admits the same entries in the same order, following the exact
/// prefix algorithm (whole entries, in rank order, until the limit or the
/// character cap would be exceeded) rather than anything order-sensitive to
/// iteration or collection internals.
/// </summary>
public sealed class MemoryContextBudgetTests
{
    private static MemoryRecord CreateRecord(string id, string body, IReadOnlyList<string>? tags = null) => new()
    {
        Id = id,
        Type = "fact",
        Tags = tags ?? [],
        Body = body,
        CreatedAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero),
        UpdatedAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero),
        CreatedBy = "test-agent"
    };

    [Fact]
    public void Select_Should_AdmitWholeEntriesInRankOrder_When_UnderBothCaps()
    {
        // arrange
        var candidates = new[]
        {
            CreateRecord("mem-01", "First."),
            CreateRecord("mem-02", "Second."),
            CreateRecord("mem-03", "Third.")
        };

        // act
        var selection = MemoryContextBudget.Select(candidates, limit: 50, maxChars: 20000);

        // assert
        Assert.Equal(["mem-01", "mem-02", "mem-03"], selection.Entries.Select(e => e.Id));
        Assert.Null(selection.OmittedEntryId);
    }

    [Fact]
    public void Select_Should_StopAtLimit_WithoutConsideringLaterCandidates()
    {
        // arrange
        var candidates = new[]
        {
            CreateRecord("mem-01", "First."),
            CreateRecord("mem-02", "Second."),
            CreateRecord("mem-03", "Third.")
        };

        // act
        var selection = MemoryContextBudget.Select(candidates, limit: 2, maxChars: 20000);

        // assert
        Assert.Equal(["mem-01", "mem-02"], selection.Entries.Select(e => e.Id));
    }

    [Fact]
    public void Select_Should_StopBeforeExceedingMaxChars_RatherThanSkippingToASmallerLaterEntry()
    {
        // arrange: the second entry's rendering alone would fit, but it is
        // never considered because the algorithm stops at the first entry
        // that would exceed the budget rather than skipping ahead.
        var first = CreateRecord("mem-01", new string('a', 40));
        var second = CreateRecord("mem-02", "x");
        var candidates = new[] { first, second };
        var maxChars = MemoryContextRenderer.RenderEntry(first).Length; // exactly fits the first, no room for a second

        // act
        var selection = MemoryContextBudget.Select(candidates, limit: 50, maxChars: maxChars);

        // assert
        Assert.Equal(["mem-01"], selection.Entries.Select(e => e.Id));
        Assert.Null(selection.OmittedEntryId);
    }

    [Fact]
    public void Select_Should_NeverTruncateAnAdmittedEntry()
    {
        // arrange
        const string body = "A body with several words that must survive intact.";
        var candidates = new[] { CreateRecord("mem-01", body) };

        // act
        var selection = MemoryContextBudget.Select(candidates, limit: 50, maxChars: 20000);

        // assert
        var admitted = Assert.Single(selection.Entries);
        Assert.Equal(body, admitted.Body);
    }

    [Fact]
    public void Select_Should_ReturnNoEntriesAndReportOmission_When_FirstCandidateAloneExceedsBudget()
    {
        // arrange
        var oversized = CreateRecord("mem-01", new string('a', 100));
        var candidates = new[] { oversized, CreateRecord("mem-02", "short") };

        // act
        var selection = MemoryContextBudget.Select(candidates, limit: 50, maxChars: 10);

        // assert
        Assert.Empty(selection.Entries);
        Assert.Equal("mem-01", selection.OmittedEntryId);
    }

    [Fact]
    public void Select_Should_BeDeterministic_When_CalledRepeatedlyWithTheSameInputs()
    {
        // arrange
        var candidates = new[]
        {
            CreateRecord("mem-01", "First entry body."),
            CreateRecord("mem-02", "Second entry body."),
            CreateRecord("mem-03", "Third entry body."),
            CreateRecord("mem-04", "Fourth entry body.")
        };

        // act
        var results = Enumerable.Range(0, 25)
            .Select(_ => MemoryContextBudget.Select(candidates, limit: 3, maxChars: 90))
            .ToList();

        // assert
        var expectedIds = results[0].Entries.Select(e => e.Id).ToList();

        foreach (var selection in results)
        {
            Assert.Equal(expectedIds, selection.Entries.Select(e => e.Id));
            Assert.Equal(results[0].OmittedEntryId, selection.OmittedEntryId);
        }
    }
}
