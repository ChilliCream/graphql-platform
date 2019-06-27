using System;
using System.Buffers;
using System.Threading;
using System.Security.Cryptography;

namespace HotChocolate.Language
{
    public class Sha1DocumentHashProvider
        : IDocumentHashProvider
    {
        private ThreadLocal<SHA1> _sha =
            new ThreadLocal<SHA1>(() => SHA1.Create());

        public string ComputeHash(ReadOnlySpan<byte> document)
        {
            // TODO : with netcoreapp 3.0 we do not need that anymore.
            byte[] rented = ArrayPool<byte>.Shared.Rent(document.Length);

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
