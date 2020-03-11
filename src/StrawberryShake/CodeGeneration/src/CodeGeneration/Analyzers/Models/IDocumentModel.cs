using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface IDocumentModel
    {
        IReadOnlyList<ITypeModel> Types { get; }

        IReadOnlyList<ParserModel> ResultParsers { get; }
    }
}
