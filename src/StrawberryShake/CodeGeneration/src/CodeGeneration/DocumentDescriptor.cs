using System;

namespace StrawberryShake.CodeGeneration
{
    public class DocumentDescriptor
        : ICodeDescriptor
    {
        private readonly byte[] _hashAlgorithm;
        private readonly byte[] _hash;
        private readonly byte[] _document;

        public DocumentDescriptor(
            string name,
            byte[] hashAlgorithm,
            byte[] hash,
            byte[] document,
            string originalDocument)
        {
            Name = name;
            _hashAlgorithm = hashAlgorithm;
            _hash = hash;
            _document = document;
            OriginalDocument = originalDocument;
        }

        public string Name { get; }

        public ReadOnlySpan<byte> HashAlgorithm => _hashAlgorithm;

        public ReadOnlySpan<byte> Hash => _hash;

        public ReadOnlySpan<byte> Document => _document;

        public string OriginalDocument { get; }
    }
}
