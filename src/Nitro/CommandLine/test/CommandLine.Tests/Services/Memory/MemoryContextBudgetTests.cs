using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// Given the same ranked candidates, limit, and character budget,
/// <see cref="MemoryContextBudget.Select"/> always admits the same entries
/// in the same order: whole entries in rank order until the limit or the
/// character cap would be exceeded.
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
        // arrange
        var first = CreateRecord("mem-01", new string('a', 40));
        var oversized = CreateRecord("mem-02", new string('b', 100));
        var smaller = CreateRecord("mem-03", "x");
        var candidates = new[] { first, oversized, smaller };
        var maxChars = MemoryContextRenderer.RenderEntry(first).Length
            + MemoryContextRenderer.Separator.Length
            + MemoryContextRenderer.RenderEntry(smaller).Length;

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
        var smaller = CreateRecord("mem-02", "short");
        var candidates = new[] { oversized, smaller };
        var maxChars = MemoryContextRenderer.RenderEntry(smaller).Length;

        // act
        var selection = MemoryContextBudget.Select(candidates, limit: 50, maxChars: maxChars);

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
