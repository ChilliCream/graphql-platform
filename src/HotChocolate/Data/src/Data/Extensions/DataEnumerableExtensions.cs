namespace HotChocolate.Data;

public static class DataEnumerableExtensions
{
    /// <summary>
    /// Wraps the <see cref="IEnumerable{T}"/> with <see cref="IQueryableExecutable{T}"/> to help
    /// the execution engine to execute it more efficient
    /// </summary>
    /// <param name="source">The source of the <see cref="IExecutable"/></param>
    /// <typeparam name="T">The type parameter</typeparam>
    /// <returns>The wrapped object</returns>
    public static IQueryableExecutable<T> AsExecutable<T>(this IEnumerable<T> source)
        => Executable.From(source.AsQueryable());

    /// <summary>
    /// Wraps the <see cref="IQueryable"/> with <see cref="IQueryableExecutable{T}"/> to help the
    /// execution engine to execute it more efficient
    /// </summary>
    /// <param name="source">The source of the <see cref="IExecutable"/></param>
    /// <typeparam name="T">The type parameter</typeparam>
    /// <returns>The wrapped object</returns>
    public static IQueryableExecutable<T> AsExecutable<T>(this IQueryable<T> source)
        => Executable.From(source);

    /// <summary>
    /// Unwraps the <see cref="IExecutable"/> to get the <see cref="IQueryable{T}"/>.
    /// </summary>
    /// <param name="executable">
    /// The executable that should be unwrapped to get the <see cref="IQueryable{T}"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type parameter of the <see cref="IQueryable{T}"/> that should be returned.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IQueryable{T}"/> that is wrapped by the executable.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Throws an <see cref="InvalidOperationException"/> if the source of the executable is not an
    /// <see cref="IQueryable{T}"/>.
    /// </exception>
    public static IQueryable<T> AsQueryable<T>(this IExecutable<T> executable)
        => executable.Source as IQueryable<T> ??
            throw new InvalidOperationException(
                "The source of the executable is not an IQueryable<T>.");
}
