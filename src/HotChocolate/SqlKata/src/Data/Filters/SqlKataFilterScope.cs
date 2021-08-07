using System.Collections.Generic;
using HotChocolate.Data.Filters;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <inheritdoc />
    public class SqlKataFilterScope
        : FilterScope<Query>
    {
        public Stack<TableInfo> TableInfo { get; } = new();

        public Stack<IFilterField> Fields { get; } = new();
    }

    public class TableInfo
    {
        public TableInfo(string tableName, string alias)
        {
            TableName = tableName;
            Alias = alias;
        }

        public string TableName { get; }

        public string Alias { get; }
    }
}
