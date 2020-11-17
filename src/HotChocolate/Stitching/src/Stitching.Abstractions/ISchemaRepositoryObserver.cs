using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching
{
    /// <summary>
    /// The schema repository observer monitors a schema repository for changes and notifies the
    /// Hot Chocolate gateway of them.
    /// </summary>
    public interface ISchemaRepositoryObserver : IAsyncDisposable
    {
        /// <summary>
        /// This event is raised when a schema definition has been updated.
        /// </summary>
        event EventHandler SchemaUpdated;

        /// <summary>
        /// Gets the available schema version.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<RemoteSchemaDefinition> GetSchemas();
    }
}
