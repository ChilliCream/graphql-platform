using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    public static class EntityFrameworkEnumerableExtensions
    {
        /// <summary>
        /// Creates an entity framework executable for a <see cref="DbSet{T}"/>
        /// </summary>
        /// <param name="source">The <see cref="DbSet{T}"/>.</param>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>
        /// Returns an <see cref="IExecutable{T}"/>.
        /// </returns>
        public static IExecutable<T> AsExecutable<T>(
            this DbSet<T> source) where T : class  =>
            new EntityFrameworkExecutable<T>(source);

        /// <summary>
        /// Creates an entity framework executable for a <see cref="IQueryable{T}"/>
        /// </summary>
        /// <param name="source">The <see cref="IQueryable{T}"/>.</param>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>
        /// Returns an <see cref="IExecutable{T}"/>.
        /// </returns>
        public static IExecutable<T> AsEntityFrameworkExecutable<T>(
            this IQueryable<T> source) =>
            new EntityFrameworkExecutable<T>(source);
    }
}
