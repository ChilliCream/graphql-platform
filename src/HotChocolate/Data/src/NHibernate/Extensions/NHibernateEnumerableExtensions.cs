using System.Linq;


namespace HotChocolate.Data
{
    public static class NHibernateEnumerableExtensions
    {
        /// <summary>
        /// Creates an entity framework executable for a <see cref="IQueryable{T}"/>
        /// </summary>
        /// <param name="source">The <see cref="IQueryable{T}"/>.</param>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>
        /// Returns an <see cref="IExecutable{T}"/>.
        /// </returns>
        public static IExecutable<T> AsNhibernateExecutable<T>(
            this IQueryable<T> source) =>
            new NHibernateExecutable<T>(source);
    }
}
