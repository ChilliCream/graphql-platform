using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Stitching
{
    /// <summary>
    /// Represents a Gateway schema repository.
    /// </summary>
    public interface ISchemaRepository
    {
        /// <summary>
        /// Gets the name of this repository.
        /// </summary>
        NameString Name { get; }

        /// <summary>
        /// Publishes the repository to a gateway schema repository
        /// from which a Hot Chocolate schema stitching gateway
        /// can pull the schema.
        /// </summary>
        /// <param name="schema">
        /// The remote schema definition.
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken" />.
        /// </param>
        ValueTask PublishSchemaAsync(
            RemoteSchemaDefinition schema,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new repository observer.
        /// </summary>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken" />.
        /// </param>
        /// <returns>
        /// Returns the new repository observer.
        /// </returns>
        ValueTask<ISchemaRepositoryObserver> CreateObserverAsync(
            CancellationToken cancellationToken = default);
    }
}
