using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface IDocumentModel
    {
        IReadOnlyList<OperationModel> Operations { get; }

        IReadOnlyList<ParserModel> Parsers { get; }

        DocumentNode Original { get; }

        DocumentNode Optimized { get; }

        string HashAlgorithm { get; }

        string Hash { get; }
    }
}
