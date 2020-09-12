using System;
using System.Reflection;

namespace HotChocolate.Data.Sorting
{
    public static class SortFieldExtensions
    {
        public static Type GetReturnType(
            this ISortField field)
        {
            if (field.Member is PropertyInfo propertyInfo)
            {
                return propertyInfo.PropertyType;
            }

            if (field.Member is MethodInfo methodInfo)
            {
                return methodInfo.ReturnType;
            }

            throw new InvalidOperationException();
        }
    }
}
