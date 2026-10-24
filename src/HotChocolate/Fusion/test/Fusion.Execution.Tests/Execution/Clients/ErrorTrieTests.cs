namespace HotChocolate.Fusion.Execution.Clients;

public sealed class ErrorTrieTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(100)]
    public void TryGetIndex_Should_MatchDictionaryLookup_When_EntriesAreMutated(int index)
    {
        // arrange
        var child = new ErrorTrie();
        var trie = new ErrorTrie();
        var states = new List<(bool ExpectedFound, bool ActualFound, bool SameValue)>();

        // act
        Record();
        trie.Add(3, child);
        Record();
        trie[3] = new ErrorTrie();
        Record();
        trie.Add(100, child);
        Record();
        trie.Remove(3);
        Record();
        trie.Clear();
        Record();
        trie.Add(new IndexKey(index), child);
        Record();
        trie.Clear();
        trie.Add("field", child);
        Record();

        // assert
        Assert.All(states, state =>
        {
            Assert.Equal(state.ExpectedFound, state.ActualFound);
            Assert.True(state.SameValue);
        });

        void Record()
        {
            var expectedFound = trie.TryGetValue(index, out var expected);
            var actualFound = trie.TryGetIndex(index, out var actual);
            states.Add((expectedFound, actualFound, ReferenceEquals(expected, actual)));
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(499)]
    public void TryGetIndex_Should_MatchDictionaryLookup_When_RemovedEntriesLeaveLargeStorage(int index)
    {
        // arrange
        var trie = new ErrorTrie();
        var child = new ErrorTrie();
        for (var i = 0; i < 500; i++)
        {
            trie.Add(i, child);
        }

        for (var i = 0; i < 499; i++)
        {
            trie.Remove(i);
        }

        // act
        var expectedFound = trie.TryGetValue(index, out var expected);
        var actualFound = trie.TryGetIndex(index, out var actual);

        // assert
        Assert.Equal(expectedFound, actualFound);
        Assert.Same(expected, actual);
    }

    private sealed class IndexKey(int index)
    {
        public override bool Equals(object? obj) => obj is int value && value == index;

        public override int GetHashCode() => index;
    }
}
