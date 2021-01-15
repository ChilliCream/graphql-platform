using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    /// <summary>
    /// Represents all models that are needed to generate a client.
    /// </summary>
    public sealed class ClientModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ClientModel" />
        /// </summary>
        /// <param name="documents">The document models.</param>
        /// <param name="types">The type models.</param>
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
