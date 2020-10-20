using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface ISchemaRepositoryObserver : IAsyncDisposable
    {
        event EventHandler SchemaUpdated;

        Task InitializeAsync(CancellationToken cancellationToken);

        IReadOnlyList<RemoteSchema> GetSchemas();
    }

    public interface ISchemaRepository
    {
        Task PublishSchemaAsync(RemoteSchema schema);
    }

    /// <summary>
    /// Describes a remote schema.
    /// </summary>
    public class RemoteSchema
    {
        /// <summary>
        /// Gets the name of the schema.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the main schema documents.
        /// </summary>
        public DocumentNode Document { get; }

        /// <summary>
        /// Gets the documents that descrobe how type are being merged 
        /// into types from other services.
        /// </summary>
        public IReadOnlyList<DocumentNode> ExtensionDocuments { get; }
    }

    /*
        extend schema 
            @_removeType(name: "abc" schema: "{optional}")
            @_renameType(name: "abc", newName: "def" schema: "{optional}")
            @_renameField(type: "abc" name: "abc", newName: "def" schema: "{optional}")
            @_renameArgument(type: "abc" field: "abc" name: "abc", newName: "def" schema: "{optional}")
            @_removeQueryType(schema: "{optional}")
            @_removeMutationType(schema: "{optional}")
            @_removeSubscriptionType(schema: "{optional}")
    */
}