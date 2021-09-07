
namespace HotChoclate.Data
{
    using System;
    using global::NHibernate;
    using global::NHibernate.SqlCommand;

    public class AuditTrailInterceptor : EmptyInterceptor
    {
        public override SqlString OnPrepareStatement(SqlString sql)
        {
            Console.WriteLine(sql.ToString());
            return sql;
        }

    }
}
