using HotChocolate.Data.Pagination;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

/// <summary>
/// This extension class provides a set of methods to
/// create entity framework optimized executables.
/// </summary>
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
    public static IQueryableExecutable<T> AsDbContextExecutable<T>(
        this DbSet<T> source) where T : class
        => new EfQueryableExecutable<T>(source);

    /// <summary>
    /// Creates an entity framework executable for a <see cref="IQueryable{T}"/>
    /// </summary>
    /// <param name="source">The <see cref="IQueryable{T}"/>.</param>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>
    /// Returns an <see cref="IExecutable{T}"/>.
    /// </returns>
    public static IQueryableExecutable<T> AsDbContextExecutable<T>(
        this IQueryable<T> source)
        => new EfQueryableExecutable<T>(source);
}
