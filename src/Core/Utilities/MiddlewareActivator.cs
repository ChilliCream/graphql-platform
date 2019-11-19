using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Utilities.Properties;

namespace HotChocolate.Utilities
{
    internal delegate T MiddlewareFactory<T, TRequestDelegate>(
        IServiceProvider services,
        TRequestDelegate next);

    internal delegate Task ClassQueryDelegate<T, TContext>(
        TContext context,
        IServiceProvider services,
        T middleware);

    internal static class MiddlewareActivator
    {
        internal static MiddlewareFactory<TMiddleware, TRequestDelegate>
            CompileFactory<TMiddleware, TRequestDelegate>(params object[] customParameter)
        {
            ParameterExpression services =
                Expression.Parameter(typeof(IServiceProvider));
            ParameterExpression nextDelegate =
                Expression.Parameter(typeof(TRequestDelegate));

            NewExpression createInstance = CreateMiddleware<TRequestDelegate>(
                typeof(TMiddleware).GetTypeInfo(), services, nextDelegate, customParameter);

            return Expression
                .Lambda<MiddlewareFactory<TMiddleware, TRequestDelegate>>(
                    createInstance, services, nextDelegate)
                .Compile();
        }

        internal static ClassQueryDelegate<TMiddleware, TContext>
            CompileMiddleware<TMiddleware, TContext>(params object[] customParameters)
        {
            TypeInfo middlewareType = typeof(TMiddleware).GetTypeInfo();
            MethodInfo method = middlewareType.GetDeclaredMethod("InvokeAsync")
                ?? middlewareType.GetDeclaredMethod("Invoke");

            if (method == null)
            {
                throw new NotSupportedException(
                    UtilityResources.MiddlewareActivator_NoInvokeMethod);
            }

            ParameterExpression context =
                Expression.Parameter(typeof(TContext));
            ParameterExpression services =
                Expression.Parameter(typeof(IServiceProvider));
            ParameterExpression middlewareInstance = Expression.Parameter(
                middlewareType.AsType());

            MethodCallExpression middlewareCall = Expression.Call(
                middlewareInstance,
                method,
                CreateParameters<TContext>(method, services, context, customParameters));

            return Expression.Lambda<ClassQueryDelegate<TMiddleware, TContext>>(
                middlewareCall, context, services, middlewareInstance)
                .Compile();
        }

        private static NewExpression CreateMiddleware<TRequestDelegate>(
            TypeInfo middleware,
            ParameterExpression services,
            Expression next,
            params object[] customParameters)
        {
            ConstructorInfo constructor = CreateConstructor(middleware);
            IEnumerable<Expression> arguments =
                CreateParameters<TRequestDelegate>(constructor, services, next, customParameters);
            return Expression.New(constructor, arguments);
        }

        private static ConstructorInfo CreateConstructor(TypeInfo middleware)
        {
            ConstructorInfo[] constructors = middleware.DeclaredConstructors
                .Where(t => t.IsPublic).ToArray();
            if (constructors.Length == 1)
            {
                return constructors[0];
            }

            throw new NotSupportedException(
                UtilityResources.MiddlewareActivator_OneConstructor);
        }

        private static IEnumerable<Expression> CreateParameters<TContext>(
            MethodInfo invokeMethod,
            ParameterExpression services,
            Expression context,
            params object[] customParameters)
        {
            var custom = new Dictionary<Type, Expression>()
            {
                { typeof(TContext), context }
            };

            return CreateParameters(
                invokeMethod.GetParameters(),
                services,
                AddCustomParameter(custom, customParameters));
        }

        private static IEnumerable<Expression>
            CreateParameters<TRequestDelegate>(
                ConstructorInfo constructor,
                ParameterExpression services,
                Expression next,
                params object[] customParameters)
        {
            var custom = new Dictionary<Type, Expression>()
            {
                { typeof(TRequestDelegate), next }
            };

            return CreateParameters(
                constructor.GetParameters(),
                services,
                AddCustomParameter(custom, customParameters));
        }

        private static IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            ParameterExpression services,
            IDictionary<Type, Expression> custom)
        {
            MethodInfo getService = typeof(IServiceProvider)
                .GetTypeInfo()
                .GetDeclaredMethod("GetService");

            foreach (ParameterInfo parameter in parameters)
            {
                if (custom.TryGetValue(parameter.ParameterType,
                    out Expression expression))
                {
                    yield return expression;
                }
                else
                {
                    yield return Expression.Convert(Expression.Call(
                        services,
                        getService,
                        Expression.Constant(parameter.ParameterType)),
                        parameter.ParameterType);
                }
            }
        }

        private static Dictionary<Type, Expression> AddCustomParameter(
            Dictionary<Type, Expression> customBase,
            params object[] customParameters)
        {
            foreach (var param in customParameters)
            {
                if (param != null)
                {
                    customBase[param.GetType()] = Expression.Constant(param);
                }
            }
            return customBase;
        }
    }
}
