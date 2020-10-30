using System.Collections.Generic;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types
{
    public static class EntityFrameworkEnumerableExtensions
    {
        public static IExecutable<T> AsExecutable<T>(
            this DbSet<T> source) where T : class
        {
            return new EntityFrameworkExecutable<T>(source);
        }
        
        public static IExecutable<T> AsEntityFrameworkExecutable<T>(
            this IEnumerable<T> source) 
        {
            return new EntityFrameworkExecutable<T>(source);
        }
    }
}
