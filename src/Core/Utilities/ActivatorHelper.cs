using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Utilities
{
    internal class ActivatorHelper
    {
        public static Func<IServiceProvider, object> CreateInstanceFactory(
            TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            var services = Expression.Parameter(typeof(IServiceProvider));
            NewExpression newInstance = CreateNewInstance(typeInfo, services);
            return Expression.Lambda<Func<IServiceProvider, object>>(
                newInstance, services).Compile();
        }

        public static Func<IServiceProvider, T> CreateInstanceFactory<T>(
            TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            var services = Expression.Parameter(typeof(IServiceProvider));
            NewExpression newInstance = CreateNewInstance(typeInfo, services);
            return Expression.Lambda<Func<IServiceProvider, T>>(
                newInstance, services).Compile();
        }

        public static Func<IServiceProvider, T> CreateInstanceFactory<T>()
            where T : class
        {
            return CreateInstanceFactory<T>(typeof(T).GetTypeInfo());
        }

        private static NewExpression CreateNewInstance(
            TypeInfo typeInfo,
            ParameterExpression services)
        {
            ConstructorInfo constructor = ResolveCunstructor(typeInfo);
            IEnumerable<Expression> arguments = CreateParameters(
                constructor.GetParameters(), services);
            return Expression.New(constructor, arguments);
        }

        private static ConstructorInfo ResolveCunstructor(TypeInfo typeInfo)
        {
            var constructors = typeInfo.DeclaredConstructors
                .Where(t => t.IsPublic).ToArray();

            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            throw new NotSupportedException(
                "The specified class must have exactly " +
                "one public constructor.");
        }

        private static IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            ParameterExpression services)
        {
            MethodInfo getService = typeof(IServiceProvider)
                .GetTypeInfo()
                .GetDeclaredMethod("GetService");

            foreach (ParameterInfo parameter in parameters)
            {
                yield return Expression.Convert(Expression.Call(
                    services,
                    getService,
                    Expression.Constant(parameter.ParameterType)),
                    parameter.ParameterType);
            }
        }
    }


}
