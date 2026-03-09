using System.Buffers;

namespace HotChocolate.Language;

public class MD5DocumentHashProviderTests
{
    [Fact]
    public void HashAsBase64()
    {
        var content = "abc"u8.ToArray();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Base64);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("kAFQmDzST7DWlj99KOF_cg");
    }

    [Fact]
    public void HashAsHex()
    {
        var content = "abc"u8.ToArray();
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("900150983cd24fb0d6963f7d28e17f72");
    }

    [Fact]
    public void HashSequenceSingleSegmentAsBase64()
    {
        var content = new ReadOnlySequence<byte>("abc"u8.ToArray());
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Base64);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("kAFQmDzST7DWlj99KOF_cg");
    }

    [Fact]
    public void HashSequenceMultiSegmentAsBase64()
    {
        var content = SequenceHelper.CreateMultiSegment("abc"u8.ToArray());
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Base64);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("kAFQmDzST7DWlj99KOF_cg");
    }

    [Fact]
    public void HashSequenceSingleSegmentAsHex()
    {
        var content = new ReadOnlySequence<byte>("abc"u8.ToArray());
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("900150983cd24fb0d6963f7d28e17f72");
    }

    [Fact]
    public void HashSequenceMultiSegmentAsHex()
    {
        var content = SequenceHelper.CreateMultiSegment("abc"u8.ToArray());
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash.Value)
            .MatchInline("900150983cd24fb0d6963f7d28e17f72");
    }
}
