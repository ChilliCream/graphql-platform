using System.Security.Cryptography;
using System.Threading;

namespace HotChocolate.Language;

public class MD5DocumentHashProvider
    : DocumentHashProviderBase
{
    private readonly ThreadLocal<MD5> _md5 = new(MD5.Create);

    public MD5DocumentHashProvider()
        : this(HashFormat.Base64)
    {
    }

    public MD5DocumentHashProvider(HashFormat format)
        : base(format)
    {
    }

    public override string Name => "md5Hash";

    protected override byte[] ComputeHash(byte[] document, int length)
    {
        return _md5.Value!.ComputeHash(document, 0, length);
    }
}
