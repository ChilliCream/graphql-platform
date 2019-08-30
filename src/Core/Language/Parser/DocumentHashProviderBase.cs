using System;
using System.Buffers;

namespace HotChocolate.Language
{
    public abstract class DocumentHashProviderBase
        : IDocumentHashProvider
    {
        internal DocumentHashProviderBase(HashFormat format)
        {
            Format = format;
        }

        public abstract string Name { get; }

        public HashFormat Format { get; }

        public string ComputeHash(ReadOnlySpan<byte> document)
        {
            // TODO : with netcoreapp 3.0 we do not need that anymore.
            byte[] rented = ArrayPool<byte>.Shared.Rent(document.Length);
            document.CopyTo(rented);

            try
            {
                byte[] hash = ComputeHash(rented, document.Length);

                switch (Format)
                {
                    case HashFormat.Base64:
                        return Convert.ToBase64String(hash);
                    case HashFormat.Hex:
                        return BitConverter.ToString(hash)
                            .ToLowerInvariant()
                            .Replace("-", string.Empty);
                    default:
                        throw new NotSupportedException(
                            "The specified has format is not supported.");
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        protected abstract byte[] ComputeHash(byte[] document, int length);
    }
}
