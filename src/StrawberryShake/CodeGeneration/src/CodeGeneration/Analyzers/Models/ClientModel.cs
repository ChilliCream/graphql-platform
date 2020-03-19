using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

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
            HasSubscriptions =
                documents.SelectMany(t => t.Operations)
                    .Select(t => t.Operation.Operation)
                    .Any(t => t == OperationType.Subscription);

        }

        public IReadOnlyList<DocumentModel> Documents { get; }

        public IReadOnlyList<ITypeModel> Types { get; }

        public bool HasSubscriptions { get; }
    }
}
