using System.Buffers;

namespace HotChocolate.Language;

public class Sha1DocumentHashProviderTests
{
    [Fact]
    public void HashAsBase64()
    {
        var content = "abc"u8.ToArray();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Base64);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("qZk-NkcGgWq6PiVxeFDCbJzQ2J0");
    }

    [Fact]
    public void HashAsHex()
    {
        var content = "abc"u8.ToArray();
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("a9993e364706816aba3e25717850c26c9cd0d89d");
    }

    [Fact]
    public void HashSequenceSingleSegmentAsBase64()
    {
        var content = new ReadOnlySequence<byte>("abc"u8.ToArray());
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Base64);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("qZk-NkcGgWq6PiVxeFDCbJzQ2J0");
    }

    [Fact]
    public void HashSequenceMultiSegmentAsBase64()
    {
        var content = SequenceHelper.CreateMultiSegment("abc"u8.ToArray());
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Base64);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("qZk-NkcGgWq6PiVxeFDCbJzQ2J0");
    }

    [Fact]
    public void HashSequenceSingleSegmentAsHex()
    {
        var content = new ReadOnlySequence<byte>("abc"u8.ToArray());
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("a9993e364706816aba3e25717850c26c9cd0d89d");
    }

    [Fact]
    public void HashSequenceMultiSegmentAsHex()
    {
        var content = SequenceHelper.CreateMultiSegment("abc"u8.ToArray());
        var hashProvider = new Sha1DocumentHashProvider(HashFormat.Hex);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("a9993e364706816aba3e25717850c26c9cd0d89d");
    }
}
