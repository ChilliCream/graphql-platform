using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface IClientModel
    {
        IReadOnlyList<IDocumentModel> Documents { get; }

        IReadOnlyList<ITypeModel> Types { get; }
    }

    public interface IDocumentModel
    {
        IReadOnlyList<IOperationModel> Operations { get; }

        IReadOnlyList<ParserModel> Parsers { get; }

        DocumentNode Original { get; }

        DocumentNode Optimized { get; }

        string HashAlgorithm { get; }

        string Hash { get; }
    }

    public interface IOperationModel
    {

    }
}
