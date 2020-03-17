using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class ClientModel
    {
        public ClientModel(
            IReadOnlyList<DocumentModel> documents,
            IReadOnlyList<ITypeModel> types)
        {
            Documents = documents;
            Types = types;
        }

        public IReadOnlyList<DocumentModel> Documents { get; }

        public IReadOnlyList<ITypeModel> Types { get; }
    }
}
