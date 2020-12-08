using System;

namespace StrawberryShake
{
    public interface IDocument
    {
        ReadOnlySpan<byte> Body { get; }

        ReadOnlySpan<byte> HashName { get; }

        ReadOnlySpan<byte> Hash { get; }

        /// <summary>
        /// Defines operation kind.
        /// </summary>
        OperationKind Kind { get; }
    }
}
