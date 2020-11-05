using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    public static class EntityFrameworkEnumerableExtensions
    {
        public static IExecutable<T> AsExecutable<T>(
            this DbSet<T> source) where T : class
        {
            return new EntityFrameworkExecutable<T>(source);
        }

        public static IExecutable<T> AsEntityFrameworkExecutable<T>(
            this IQueryable<T> source)
        {
            return new EntityFrameworkExecutable<T>(source);
        }
    }
}
