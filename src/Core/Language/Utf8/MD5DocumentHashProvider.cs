using System;
using System.Buffers;
using System.Threading;
using System.Security.Cryptography;

namespace HotChocolate.Language
{
    public class MD5DocumentHashProvider
        : IDocumentHashProvider
    {
        private ThreadLocal<MD5> _md5 =
            new ThreadLocal<MD5>(() => MD5.Create());

        public string ComputeHash(ReadOnlySpan<byte> document)
        {
            // TODO : with netcoreapp 3.0 we do not need that anymore.
            byte[] rented = ArrayPool<byte>.Shared.Rent(document.Length);
            document.CopyTo(rented);

            try
            {
                byte[] hash = _md5.Value.ComputeHash(rented, 0, document.Length);
                return Convert.ToBase64String(hash);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
