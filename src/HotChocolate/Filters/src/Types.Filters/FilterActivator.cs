using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace HotChocolate.Types.Filters
{
    internal static class FilterActivator
    {

        internal static T CompileFilter<T>(Type filterType, string conventionName)
        {
            return CreateFilterLambda<T>(filterType.GetTypeInfo(), conventionName).Invoke();
        }

        private static Func<T> CreateFilterLambda<T>(
            TypeInfo filterType, string conventionName)
        {
            ConstructorInfo constructor = CreateConventionConstructor(filterType);
            NewExpression ctor
                = Expression.New(constructor, new[] { Expression.Constant(conventionName) });
            return Expression.Lambda<Func<T>>(ctor).Compile();
        }

        private static ConstructorInfo CreateConventionConstructor(TypeInfo filterType)
        {
            ConstructorInfo[] constructors = filterType.DeclaredConstructors
                .Where(
                    t => t.IsPublic && t.GetParameters().Length == 1 && 
                    t.GetParameters().FirstOrDefault()?.ParameterType == typeof(string))
                .ToArray();

            if (constructors.Length == 1)
            {
                return constructors[0];
            }
            // TODO add ressource
            throw new NotSupportedException();
        }
    }
}
