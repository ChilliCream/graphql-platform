using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Grpc
{
    /// <summary>
    /// Enumerable extensions.
    /// </summary>
    public static class EnumeratorExtensions
    {
        /// <summary>
        /// Convert IEnumerator<typeparamref name="T"/> to IEnumerable<typeparamref name="T"/>.
        /// </summary>
        /// <returns>The IEnumerable<typeparamref name="T"/>>.</returns>
        /// <param name="enumerator">Enumerator.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}
