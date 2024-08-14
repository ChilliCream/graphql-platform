namespace HotChocolate.Data;

public static class EntityFrameworkQueryableExtensions
{
    public static IQueryableExecutable<T> ToExecutable<T>(
        this IQueryable<T> source)
        => new EfQueryableExecutable<T>(source);
}
