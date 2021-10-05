
namespace HotChoclate.Data
{
    using System;
    using NHibernate;
    using NHibernate.SqlCommand;

    public class AuditTrailInterceptor : EmptyInterceptor
    {
        public override SqlString OnPrepareStatement(SqlString sql)
        {
            Console.WriteLine(sql.ToString());
            return sql;
        }

    }
}
