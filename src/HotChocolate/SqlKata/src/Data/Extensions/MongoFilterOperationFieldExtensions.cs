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

        public static bool HasForeignKey(this IFilterField field)
        {
            return field.ContextData.ContainsKey(SqlKataContextData.ColumnName);
        }

        public static string GetForeignKey(this IFilterField field)
        {
            string fieldName = field.Name;
            if (field.ContextData
                    .TryGetValue(SqlKataContextData.ForeignKey, out object? foreignKeyObj) &&
                foreignKeyObj is string foreignKey)
            {
                return foreignKey;
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

        public static string GetKey(
            this SqlKataFilterVisitorContext context,
            IFilterField field)
        {
            if (field.DeclaringType.ContextData
                    .TryGetValue(SqlKataContextData.KeyName, out object? keyNameObj) &&
                keyNameObj is string keyName)
            {
                return keyName;
            }

            return context.RuntimeTypes.Peek().Type.Name + "s";
        }
    }
}
