namespace HotChocolate.Fusion.Text.Json;

public class PathSegmentMemoryTests
{
    // Mirrors PathSegmentMemory's base capacity ladder so DivideLevels can be exercised as a pure
    // function without touching the static striped pools.
    private static readonly int[] s_baseLevels =
    [
        4096,
        6144,
        9216,
        13824,
        20736,
        31104,
        46656,
        69984,
        104976,
        157464,
        236196,
        354294,
        531441,
        797162,
        1195743,
        1793614,
        2690422,
        4035632,
        6053449,
        9080173,
        13620260
    ];

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(64)]
    [InlineData(128)]
    [InlineData(192)]
    [InlineData(256)]
    [InlineData(1024)]
    public void DivideLevels_Should_KeepLevelsStrictlyIncreasing_When_StripeCountIsLarge(int count)
    {
        // act
        var levels = PathSegmentMemory.DivideLevels(s_baseLevels, count);

        // assert
        Assert.NotEmpty(levels);
        for (var i = 1; i < levels.Length; i++)
        {
            Assert.True(
                levels[i] > levels[i - 1],
                $"levels must be strictly increasing but index {i} ({levels[i]}) "
                + $"was not greater than index {i - 1} ({levels[i - 1]}).");
        }
    }

    [Fact]
    public void DivideLevels_Should_DropDuplicatedFloor_When_StripeCountIs192()
    {
        // act
        // at stripe count 192 the first two base levels both divide down to the floor of 32; the
        // duplicate must be dropped so the bucket level ladder does not stall on equal adjacent levels
        var levels = PathSegmentMemory.DivideLevels(s_baseLevels, 192);

        // assert
        string.Join(", ", levels).MatchInlineSnapshot(
            "32, 48, 72, 108, 162, 243, 364, 546, 820, 1230, 1845, 2767, 4151, 6227, "
            + "9341, 14012, 21018, 31528, 47292, 70938");
    }

    [Fact]
    public void SelectPoolIndex_Should_CycleThroughAllPools_When_CalledPoolCountTimes()
    {
        // arrange
        var cursor = -1;
        var indexes = new int[5];

        // act
        for (var i = 0; i < indexes.Length; i++)
        {
            indexes[i] = PathSegmentMemory.SelectPoolIndex(ref cursor, poolCount: 4);
        }

        // assert
        // the first four calls visit every pool once, then the fifth wraps back to the first pool
        Assert.Equal(new[] { 0, 1, 2, 3, 0 }, indexes);
    }

    [Fact]
    public void SelectPoolIndex_Should_StayInRange_When_CursorOverflows()
    {
        // arrange
        var cursor = int.MaxValue - 1;
        const int poolCount = 4;
        var indexes = new int[4];

        // act
        // stepping the cursor across int overflow must never yield a negative or out-of-range index
        for (var i = 0; i < indexes.Length; i++)
        {
            indexes[i] = PathSegmentMemory.SelectPoolIndex(ref cursor, poolCount);
        }

        // assert
        Assert.All(indexes, index => Assert.InRange(index, 0, poolCount - 1));
    }
}
