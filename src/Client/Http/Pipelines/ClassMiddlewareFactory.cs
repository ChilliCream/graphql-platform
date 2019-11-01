using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    internal static class ClassMiddlewareFactory
    {
        private delegate object Factory(
            IServiceProvider services,
            OperationDelegate next);

        internal delegate Task Invoke(
            IServiceProvider services,
            IHttpOperationContext context,
            object instance);

        public static OperationMiddleware Create(Type classMiddlewareType)
        {
            if (classMiddlewareType is null)
            {
                throw new ArgumentNullException(nameof(classMiddlewareType));
            }

            TypeInfo classMiddlewareTypeInfo = classMiddlewareType.GetTypeInfo();
            MethodInfo? invokeMethod = classMiddlewareTypeInfo.GetDeclaredMethod("InvokeAsync")
                ?? classMiddlewareTypeInfo.GetDeclaredMethod("Invoke");

            if (invokeMethod is null)
            {
                throw new InvalidOperationException(
                    "Class middleware must have a `Invoke` or a `InvokeAsync` method.");
            }

            ParameterExpression services =
                Expression.Parameter(typeof(IServiceProvider));
            ParameterExpression next =
                Expression.Parameter(typeof(OperationDelegate));
            NewExpression createInstance =
                CreateMiddleware(classMiddlewareTypeInfo, services, next);

            Factory factory =
                Expression.Lambda<Factory>(
                    createInstance, services, next)
                    .Compile();

            ParameterExpression context =
                Expression.Parameter(typeof(IHttpOperationContext));
            ParameterExpression instance =
                Expression.Parameter(typeof(object));

            MethodCallExpression invokeMiddleware = Expression.Call(
                Expression.Convert(instance, classMiddlewareType),
                invokeMethod,
                CreateParameters(invokeMethod, services, context));

            Invoke invoke =
                Expression.Lambda<Invoke>(
                    invokeMiddleware, services, context, instance)
                    .Compile();

            return (s, n) =>
            {
                object obj = factory(s, n);
                return c => invoke(s, c, obj);
            };
        }

        private static NewExpression CreateMiddleware(
            TypeInfo classMiddlewareTypeInfo,
            ParameterExpression services,
            Expression next)
        {
            ConstructorInfo constructor = CreateConstructor(
                classMiddlewareTypeInfo);

            IEnumerable<Expression> arguments = CreateParameters(
                constructor, services, next);

            return Expression.New(constructor, arguments);
        }

        private static ConstructorInfo CreateConstructor(
            TypeInfo classMiddlewareTypeInfo)
        {
            ConstructorInfo constructor =
                classMiddlewareTypeInfo.DeclaredConstructors
                    .SingleOrDefault(t => t.IsPublic);

            if (constructor is null)
            {
                throw new InvalidOperationException(
                    "Class middleware must have one constructor.");
            }

            return constructor;
        }

        private static IEnumerable<Expression> CreateParameters(
             MethodInfo invokeMethod,
             ParameterExpression services,
             Expression context)
        {
            return CreateParameters(
                invokeMethod.GetParameters(),
                services,
                new Dictionary<Type, Expression>
                {
                    { typeof(IHttpOperationContext), context }
                });
        }

        private static IEnumerable<Expression> CreateParameters(
            ConstructorInfo constructor,
            ParameterExpression services,
            Expression next)
        {
            return CreateParameters(
                constructor.GetParameters(),
                services,
                new Dictionary<Type, Expression>
                {
                    { typeof(OperationDelegate), next }
                });
        }

        private static IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            ParameterExpression services,
            IDictionary<Type, Expression> custom)
        {
            MethodInfo getService = typeof(IServiceProvider)
                .GetTypeInfo()
                .GetDeclaredMethod("GetService")!;

            foreach (ParameterInfo parameter in parameters)
            {
                if (custom.TryGetValue(parameter.ParameterType,
                    out Expression? expression))
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
    }
}
