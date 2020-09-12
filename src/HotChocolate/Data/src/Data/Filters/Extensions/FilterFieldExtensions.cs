using System;
using System.Reflection;

namespace HotChocolate.Data.Filters
{
    public static class FilterFieldExtensions
    {
        public static Type GetReturnType(
            this IFilterField field)
        {
            if (field.Member is PropertyInfo propertyInfo)
            {
                return propertyInfo.PropertyType;
            }
            else if (field.Member is MethodInfo methodInfo)
            {
                return methodInfo.ReturnType;
            }

            throw new InvalidOperationException();
        }
    }
}