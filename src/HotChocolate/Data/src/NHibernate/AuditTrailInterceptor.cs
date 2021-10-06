using System;
using NHibernate;
using NHibernate.SqlCommand;

namespace HotChoclate.Data
{
    public class AuditTrailInterceptor : EmptyInterceptor
    {
        public override SqlString OnPrepareStatement(SqlString sql)
        {
            Console.WriteLine(sql.ToString());
            return sql;
        }

    }
}
