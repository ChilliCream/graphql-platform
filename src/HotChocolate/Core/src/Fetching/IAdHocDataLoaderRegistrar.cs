using System;
using GreenDonut;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// Describes a registrar to register ad-hoc dataloader. An ad-hoc dataloader is created under
    /// the hood automatically and is based on predefined dataloader types. Therefore creating a
    /// specific dataloader type isn't required.
    /// </summary>
    public interface IAdHocDataLoaderRegistrar
    {
        /// <summary>
        /// Gets a new or cached instance of a dataloader by name.
        /// </summary>
        /// <param name="name">A unique name for a specific dataloader.</param>
        /// <param name="factory">A factory to create a new dataloader instance.</param>
        /// <typeparam name="TDataLoader">A specific dataloader type.</typeparam>
        /// <returns>A cached or new instance of a dataloader.</returns>
        TDataLoader GetOrAddDataLoader<TDataLoader>(
            string name,
            Func<IBatchScheduler, TDataLoader> factory)
            where TDataLoader : IDataLoader;
    }
}
