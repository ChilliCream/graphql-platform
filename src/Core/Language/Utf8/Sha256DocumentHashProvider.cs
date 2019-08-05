using System;
using System.Buffers;
using System.Threading;
using System.Security.Cryptography;

namespace HotChocolate.Language
{
    public class Sha256DocumentHashProvider
        : IDocumentHashProvider
    {
        private readonly ThreadLocal<SHA256> _sha =
            new ThreadLocal<SHA256>(() => SHA256.Create());

        public string Name => "sha256Hash";

        public string ComputeHash(ReadOnlySpan<byte> document)
        {
            // TODO : with netcoreapp 3.0 we do not need that anymore.
            byte[] rented = ArrayPool<byte>.Shared.Rent(document.Length);
            document.CopyTo(rented);

            try
            {
                byte[] hash = _sha.Value.ComputeHash(rented, 0, document.Length);
                return Convert.ToBase64String(hash);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
