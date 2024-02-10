using System.Text;
using CookieCrumble;

namespace HotChocolate.Language;

public class MD5DocumentHashProviderTests
{
    [Fact]
    public void HashAsBase64()
    {
        var content = Encoding.UTF8.GetBytes("abc");
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Base64);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash)
            .MatchInline("kAFQmDzST7DWlj99KOF/cg==");
    }

    [Fact]
    public void HashAsHex()
    {
        var content = Encoding.UTF8.GetBytes("abc");
        var hashProvider = new MD5DocumentHashProvider(HashFormat.Hex);

        var hash = hashProvider.ComputeHash(content);

        Snapshot
            .Create()
            .Add(hash)
            .MatchInline("900150983cd24fb0d6963f7d28e17f72");
    }
}
