using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotChocolate.Data.Filters.Spatial;

public static class ToQueryStringExtensions
{
    public static string ToQueryString<TEntity>(
        this IQueryable<TEntity> query)
        where TEntity : class
    {
        var enumerator = query
            .Provider
            .Execute<IEnumerable<TEntity>>(query.Expression)
           .GetEnumerator();

        var relationalCommandCache = enumerator.Private("_relationalCommandCache");

        var selectExpression = relationalCommandCache
            .Private<SelectExpression>("_selectExpression");

        var factory = relationalCommandCache
            .Private<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");

        var sqlGenerator = factory.Create();
        var command = sqlGenerator.GetCommand(selectExpression);

        var sql = command.CommandText;
        return sql;
    }

    private static object Private(
        this object? obj,
        string privateField) =>
        obj?.GetType()?
            .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(obj) ?? throw new InvalidOperationException();

    private static T Private<T>(
        this object obj,
        string privateField)
        where T : class =>
        (T?)obj?.GetType()?
            .GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(obj) ?? throw new InvalidOperationException();
}
