using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    /// <summary>
    /// Defines a remote schema and how it shall be stitched into the Hot Chocolate gateway.
    /// </summary>
    public class RemoteSchemaDefinition
    {
        public RemoteSchemaDefinition(
            NameString name,
            DocumentNode document,
            IEnumerable<DocumentNode>? extensionDocuments = null)
        {
            Name = name;
            Document = document;
            ExtensionDocuments = extensionDocuments?.ToArray() ?? Array.Empty<DocumentNode>();
        }

        /// <summary>
        /// Gets the name of the schema.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the main schema documents.
        /// </summary>
        public DocumentNode Document { get; }

        /// <summary>
        /// Gets the documents that describes how type are being merged
        /// into types from other services.
        /// </summary>
        public IReadOnlyList<DocumentNode> ExtensionDocuments { get; }
    }
}
