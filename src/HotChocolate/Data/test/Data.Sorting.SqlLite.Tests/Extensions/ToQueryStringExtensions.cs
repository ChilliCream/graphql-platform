#if NET5_0_OR_GREATER
using System.Linq;
using Microsoft.EntityFrameworkCore;
#else
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Query;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
#endif

namespace HotChocolate.Data.Sorting
{
    public static class ToQueryStringExtensions
    {
        public static string ToQueryString<TEntity>(
            this IQueryable<TEntity> query)
            where TEntity : class
        {

#if NET5_0_OR_GREATER
            var sql = EntityFrameworkQueryableExtensions.ToQueryString(query);
#else
            using IEnumerator<TEntity> enumerator = query.Provider
                .Execute<IEnumerable<TEntity>>(query.Expression)
                .GetEnumerator();

            object relationalCommandCache = enumerator.Private("_relationalCommandCache");

            SelectExpression? selectExpression = relationalCommandCache
                .Private<SelectExpression>("_selectExpression");

            IQuerySqlGeneratorFactory? factory = relationalCommandCache
                .Private<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");

            QuerySqlGenerator? sqlGenerator = factory.Create();
            IRelationalCommand? command = sqlGenerator.GetCommand(selectExpression);
            var sql = command.CommandText;
#endif
            return sql;
        }

#if !NET5_0_OR_GREATER
        private static object Private(
            this object? obj,
            string privateField) =>
            obj?.GetType()
                ?.GetField(
                    privateField,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(obj) ??
            throw new InvalidOperationException();

        private static T Private<T>(
            this object obj,
            string privateField)
            where T : class =>
            (T?)obj?.GetType()
                ?.GetField(
                    privateField,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(obj) ??
            throw new InvalidOperationException();
#endif
    }
}
