using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class DocumentModel
    {
        public DocumentModel(
            IReadOnlyList<OperationModel> operations,
            IReadOnlyList<ParserModel> parsers,
            DocumentNode original,
            DocumentNode optimized,
            string hashAlgorithm,
            string hash)
        {
            Operations = operations;
            Parsers = parsers;
            Original = original;
            Optimized = optimized;
            HashAlgorithm = hashAlgorithm;
            Hash = hash;
        }

        public IReadOnlyList<OperationModel> Operations { get; }

        public IReadOnlyList<ParserModel> Parsers { get; }

        public DocumentNode Original { get; }

        public DocumentNode Optimized { get; }

        public string HashAlgorithm { get; }

        public string Hash { get; }
    }
}
