using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Language;

namespace MarshmallowPie
{
    public sealed class DocumentHash
        : IEquatable<DocumentHash>
    {
        public DocumentHash(
            string hash,
            string algorithm,
            HashFormat format = HashFormat.Hex)
        {
            Hash = hash;
            Algorithm = algorithm;
            Format = format;
        }

        public string Hash { get; }

        public string Algorithm { get; }

        public HashFormat Format { get; }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as DocumentHash);
        }

        public bool Equals(DocumentHash? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Format.Equals(other.Format)
                && Algorithm.Equals(other.Algorithm, StringComparison.Ordinal)
                && Hash.Equals(other.Hash, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hash, Algorithm, Format);
        }

        public override string ToString() => Hash;

        public static DocumentHash FromSourceText(string sourceText)
        {
            byte[] rentedContent = ArrayPool<byte>.Shared.Rent(4 * sourceText.Length);
            byte[] rentedHash = ArrayPool<byte>.Shared.Rent(32);

            try
            {
                Span<byte> content = rentedContent;
                Span<byte> hash = rentedHash;

                int bytesConsumed = Encoding.UTF8.GetBytes(sourceText.AsSpan(), content);

                using var sha = SHA256.Create();

                if (sha.TryComputeHash(content.Slice(0, bytesConsumed), hash, out int written))
                {
                    return new DocumentHash(
                        BitConverter.ToString(rentedHash, 0, written),
                        "SHA256",
                        HashFormat.Hex);

                }

                throw new InvalidOperationException("Unable to compute the hash.");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedContent);
                ArrayPool<byte>.Shared.Return(rentedHash);
            }
        }
    }
}
