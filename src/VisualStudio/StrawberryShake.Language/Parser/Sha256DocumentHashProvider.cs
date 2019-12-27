using System.Threading;
using System.Security.Cryptography;

namespace HotChocolate.Language
{
    public class Sha256DocumentHashProvider
        : DocumentHashProviderBase
    {
        private readonly ThreadLocal<SHA256> _sha =
            new ThreadLocal<SHA256>(() => SHA256.Create());

        public Sha256DocumentHashProvider()
           : this(HashFormat.Base64)
        {
        }

        public Sha256DocumentHashProvider(HashFormat format)
            : base(format)
        {
        }

        public override string Name => "sha256Hash";

        protected override byte[] ComputeHash(byte[] document, int length)
        {
            return _sha.Value!.ComputeHash(document, 0, length);
        }
    }
}
