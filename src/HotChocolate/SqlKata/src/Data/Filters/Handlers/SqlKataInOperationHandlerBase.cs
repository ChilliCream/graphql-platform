using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.SqlKata.Filters
{
    public abstract class SqlKataInOperationHandlerBase
        : SqlKataOperationHandlerBase
    {
        protected static (bool HasNull, List<object> Values) ExtractValues(IList list)
        {
            bool hasNull = false;
            List<object> result = new();
            foreach (var item in list)
            {
                if (item is not null)
                {
                    result.Add(item);
                }
                else
                {
                    hasNull = true;
                }
            }

            return (hasNull, result);
        }
    }
}
