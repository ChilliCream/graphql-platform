using System;
using System.Collections.Generic;
using static HotChocolate.Data.Properties.EntityFrameworkResources;

namespace HotChocolate.Data
{
    internal static class ExpressionHelper
    {
        public static TContextData GetLocalState<TContextData>(
            IReadOnlyDictionary<string, object> contextData,
            string key)
        {
            if (contextData.TryGetValue(key, out var value) && value is not null)
            {
                if (value is TContextData v)
                {
                    return v;
                }
            }

            throw new ArgumentException(
                string.Format(DbContextParameterExpressionBuilder_GetLocalState_DbContextNotFound, key));
        }
    }
}
