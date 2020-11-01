using System.Linq;

namespace HotChocolate.Data
{
    public interface IQueryableExecutable<T> : IExecutable<T>
    {
        /// <summary>
        /// The current state of the executable
        /// </summary>
        new IQueryable<T> Source { get; }

        /// <summary>
        /// Is true if <see cref="IQueryableExecutable{T}.Source"/> source is a in memory query and
        /// false if it is a database query
        /// </summary>
        bool InMemory { get; }

        /// <summary>
        /// Returns a new enumerable executable with the provided source
        /// </summary>
        /// <param name="source">The source that should be set</param>
        /// <returns>The new instance of an enumerable executable</returns>
        QueryableExecutable<T> WithSource(IQueryable<T> source);
    }
}
