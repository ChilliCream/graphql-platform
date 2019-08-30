using System;
using System.Buffers;

namespace HotChocolate.Language
{
    public abstract class DocumentHashProviderBase
        : IDocumentHashProvider
    {
        private readonly HashRepresentation _representation;

        internal DocumentHashProviderBase(HashRepresentation representation)
        {
            _representation = representation;
        }

        public abstract string Name { get; }

        public string ComputeHash(ReadOnlySpan<byte> document)
        {
            // TODO : with netcoreapp 3.0 we do not need that anymore.
            byte[] rented = ArrayPool<byte>.Shared.Rent(document.Length);
            document.CopyTo(rented);

            try
            {
                byte[] hash = ComputeHash(rented, document.Length);

                switch (_representation)
                {
                    case HashRepresentation.Base64:
                        return Convert.ToBase64String(hash);
                    case HashRepresentation.Hex:
                        return BitConverter.ToString(hash)
                            .ToLowerInvariant()
                            .Replace("-", string.Empty);
                    default:
                        throw new NotSupportedException(
                            "The specified has representation is not supported.");
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
