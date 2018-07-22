using System.Collections.Generic;

namespace HotChocolate.Runtime
{
    public interface IDataLoaderState
    {
        /// <summary>
        /// Gets the data loaders that have been requested since the last reset.
        /// </summary>
        /// <value></value>
        IEnumerable<DataLoaderInfo> Touched { get; }

        /// <summary>
        /// Get a specific data loader.
        /// </summary>
        /// <param name="key">
        /// The data loader key.
        /// </param>
        /// <typeparam name="T">
        /// The type of the data loader.
        /// </typeparam>
        /// <returns>
        /// Returns the data loader that is associated with the specified key.
        /// </returns>
        T GetDataLoader<T>(string key);

        /// <summary>
        /// Resets the touched data loader collection.
        /// </summary>
        void Reset();
    }
}
