using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface IClientModel
    {
        IReadOnlyList<IDocumentModel> Documents { get; }

        IReadOnlyList<ITypeModel> Types { get; }
    }
}
