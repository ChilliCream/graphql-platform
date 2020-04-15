using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotChocolate.Types.Filters
{
    public static class IQueryableExtensions
    {

        public static IObjectFieldDescriptor MatchSql<TEntity>(
            this IObjectFieldDescriptor descriptor) where TEntity : class
        {
            descriptor.Use(next => async context =>
            {
                await next(context);
                if (context.Result is IQueryable<TEntity> result)
                {
                    context.Service<MatchSqlHelper>().Sql = result.ToQueryString();
                }
            });

            return descriptor;
        }


        public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        {
            IEnumerator<TEntity> enumerator = query.Provider.Execute<IEnumerable<TEntity>>(
                query.Expression).GetEnumerator();
            var relationalCommandCache = enumerator.Private("_relationalCommandCache");
            SelectExpression selectExpression
                = relationalCommandCache.Private<SelectExpression>("_selectExpression");
            IQuerySqlGeneratorFactory factory
                = relationalCommandCache.Private<IQuerySqlGeneratorFactory>(
                    "_querySqlGeneratorFactory");

            QuerySqlGenerator sqlGenerator = factory.Create();
            IRelationalCommand command = sqlGenerator.GetCommand(selectExpression);

            return command.CommandText;
        }

        private static object Private(this object obj, string privateField)
            => obj?.GetType()
                .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(obj);

        private static T Private<T>(this object obj, string privateField)
            => (T)obj?.GetType()
                .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(obj);
    }
}
