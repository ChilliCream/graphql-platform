using HotChocolate.Data.Filters;
using HotChocolate.Data.SqlKata.Filters;

namespace HotChocolate.Data.SqlKata
{
    internal static class SqlKataFilterOperationFieldExtensions
    {
        public static string GetColumnName(this IFilterField field)
        {
            string fieldName = field.Name;
            if (field.ContextData
                    .TryGetValue(SqlKataContextData.ColumnName, out object? columnNameObj) &&
                columnNameObj is string columnName)
            {
                return columnName;
            }

            if (field.Member is { } p)
            {
                fieldName = p.Name;
            }

            return fieldName;
        }

        public static string GetTableName(
            this SqlKataFilterVisitorContext context,
            IFilterField field)
        {
            if (field.DeclaringType.ContextData
                    .TryGetValue(SqlKataContextData.TableName, out object? tableNameObj) &&
                tableNameObj is string tableName)
            {
                return tableName;
            }

            return context.RuntimeTypes.Peek().Type.Name + "s";
        }
    }
}
