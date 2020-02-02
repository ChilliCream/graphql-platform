using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut
{
    /// <summary>
    /// A <c>DataLoader</c> creates a public API for loading data from a
    /// particular data back-end with unique keys such as the `id` column of a
    /// SQL table or document name in a MongoDB database, given a batch loading
    /// function. -- facebook
    ///
    /// Each <c>DataLoader</c> instance contains a unique memoized cache. Use
    /// caution when used in long-lived applications or those which serve many
    /// users with different access permissions and consider creating a new
    /// instance per web request. -- facebook
    ///
    /// A default <c>DataLoader</c> implementation which supports automatic and
    /// manual batch dispatching. Also this implementation is using the default
    /// cache implementation which useses the LRU (Least Recently Used) caching
    /// algorithm for keeping track on which item has to be discarded first.
    /// </summary>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <typeparam name="TValue">A value type.</typeparam>
    public sealed class DataLoader<TKey, TValue>
        : DataLoaderBase<TKey, TValue>
    {
        private readonly FetchDataDelegate<TKey, TValue> _fetch;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataLoader{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="fetch">
        /// A delegate to fetch data batches which will be invoked every time
        /// when trying to setup a new batch request.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="fetch"/> is <c>null</c>.
        /// </exception>
        public DataLoader(FetchDataDelegate<TKey, TValue> fetch)
            : this(new DataLoaderOptions<TKey>(), fetch)
        { }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataLoader{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="options">
        /// An options object to configure the behavior of this particular
        /// <see cref="DataLoader{TKey, TValue}"/>.
        /// </param>
        /// <param name="fetch">
        /// A delegate to fetch data batches which will be invoked every time
        /// when trying to setup a new batch request.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="options"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="fetch"/> is <c>null</c>.
        /// </exception>
        public DataLoader(
            DataLoaderOptions<TKey> options,
            FetchDataDelegate<TKey, TValue> fetch)
                : base(options)
        {
            _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        }

        /// <inheritdoc />
        protected override Task<IReadOnlyList<Result<TValue>>> FetchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken)
        {
            return _fetch(keys, cancellationToken);
        }
    }
}
