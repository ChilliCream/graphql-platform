using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut
{
    /// <summary>
    /// A bunch of convenient <c>DataLoader</c> extension methods.
    /// </summary>
    public static class DataLoaderExtensions
    {
        /// <summary>
        /// Loads a single value by key. This call may return a cached value
        /// or enqueues this single request for bacthing if enabled.
        /// </summary>
        /// <param name="dataLoader">A data loader instance.</param>
        /// <param name="key">A unique key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A single result which may contain a value or information about the
        /// error which may occurred during the call.
        /// </returns>
        public static Task<object?> LoadAsync(
            this IDataLoader dataLoader,
            object key)
        {
            if (dataLoader == null)
            {
                throw new ArgumentNullException(nameof(dataLoader));
            }

            return dataLoader.LoadAsync(key, CancellationToken.None);
        }

        /// <summary>
        /// Loads multiple values by keys. This call may return cached values
        /// and enqueues requests which were not cached for bacthing if
        /// enabled.
        /// </summary>
        /// <param name="dataLoader">A data loader instance.</param>
        /// <param name="keys">A list of unique key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="keys"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A single result which may contain a value or information about the
        /// error which may occurred during the call.
        /// </returns>
        public static Task<IReadOnlyList<object?>> LoadAsync(
            this IDataLoader dataLoader,
            params object[] keys)
        {
            if (dataLoader == null)
            {
                throw new ArgumentNullException(nameof(dataLoader));
            }

            return dataLoader.LoadAsync(CancellationToken.None, keys);
        }

        /// <summary>
        /// Loads multiple values by keys. This call may return cached values
        /// and enqueues requests which were not cached for bacthing if
        /// enabled.
        /// </summary>
        /// <param name="dataLoader">A data loader instance.</param>
        /// <param name="keys">A list of unique key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="keys"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A single result which may contain a value or information about the
        /// error which may occurred during the call.
        /// </returns>
        public static Task<IReadOnlyList<object?>> LoadAsync(
            this IDataLoader dataLoader,
            IReadOnlyCollection<object> keys)
        {
            if (dataLoader == null)
            {
                throw new ArgumentNullException(nameof(dataLoader));
            }

            return dataLoader.LoadAsync(keys, CancellationToken.None);
        }

        /// <summary>
        /// Adds a new entry to the cache if not already exists.
        /// </summary>
        /// <param name="dataLoader">A data loader instance.</param>
        /// <param name="key">A cache entry key.</param>
        /// <param name="value">A cache entry value.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        public static void Set(
            this IDataLoader dataLoader,
            object key,
            object? value)
        {
            if (dataLoader == null)
            {
                throw new ArgumentNullException(nameof(dataLoader));
            }

            dataLoader.Set(key, Task.FromResult(value));
        }

        /// <summary>
        /// Loads a single value by key. This call may return a cached value
        /// or enqueues this single request for bacthing if enabled.
        /// </summary>
        /// <typeparam name="TKey">A key type.</typeparam>
        /// <typeparam name="TValue">A value type.</typeparam>
        /// <param name="dataLoader">A data loader instance.</param>
        /// <param name="key">A unique key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A single result which may contain a value or information about the
        /// error which may occurred during the call.
        /// </returns>
        public static Task<TValue> LoadAsync<TKey, TValue>(
            this IDataLoader<TKey, TValue> dataLoader,
            TKey key)
                where TKey : notnull
        {
            if (dataLoader == null)
            {
                throw new ArgumentNullException(nameof(dataLoader));
            }

            return dataLoader.LoadAsync(key, CancellationToken.None);
        }

        /// <summary>
        /// Loads multiple values by keys. This call may return cached values
        /// and enqueues requests which were not cached for bacthing if
        /// enabled.
        /// </summary>
        /// <typeparam name="TKey">A key type.</typeparam>
        /// <typeparam name="TValue">A value type.</typeparam>
        /// <param name="dataLoader">A data loader instance.</param>
        /// <param name="keys">A list of unique key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="keys"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A single result which may contain a value or information about the
        /// error which may occurred during the call.
        /// </returns>
        public static Task<IReadOnlyList<TValue>> LoadAsync<TKey, TValue>(
            this IDataLoader<TKey, TValue> dataLoader,
            params TKey[] keys)
                where TKey : notnull
        {
            if (dataLoader == null)
            {
                throw new ArgumentNullException(nameof(dataLoader));
            }

            return dataLoader.LoadAsync(CancellationToken.None, keys);
        }

        /// <summary>
        /// Loads multiple values by keys. This call may return cached values
        /// and enqueues requests which were not cached for bacthing if
        /// enabled.
        /// </summary>
        /// <typeparam name="TKey">A key type.</typeparam>
        /// <typeparam name="TValue">A value type.</typeparam>
        /// <param name="dataLoader">A data loader instance.</param>
        /// <param name="keys">A list of unique key.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="keys"/> is <c>null</c>.
        /// </exception>
        /// <returns>
        /// A single result which may contain a value or information about the
        /// error which may occurred during the call.
        /// </returns>
        public static Task<IReadOnlyList<TValue>> LoadAsync<TKey, TValue>(
            this IDataLoader<TKey, TValue> dataLoader,
            IReadOnlyCollection<TKey> keys)
                where TKey : notnull
        {
            if (dataLoader == null)
            {
                throw new ArgumentNullException(nameof(dataLoader));
            }

            return dataLoader.LoadAsync(keys, CancellationToken.None);
        }

        /// <summary>
        /// Adds a new entry to the cache if not already exists.
        /// </summary>
        /// <typeparam name="TKey">A key type.</typeparam>
        /// <typeparam name="TValue">A value type.</typeparam>
        /// <param name="dataLoader">A data loader instance.</param>
        /// <param name="key">A cache entry key.</param>
        /// <param name="value">A cache entry value.</param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="dataLoader"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        public static void Set<TKey, TValue>(
            this IDataLoader<TKey, TValue> dataLoader,
            TKey key,
            TValue value)
                where TKey : notnull
        {
            if (dataLoader == null)
            {
                throw new ArgumentNullException(nameof(dataLoader));
            }

            dataLoader.Set(key, Task.FromResult(value));
        }
    }
}
