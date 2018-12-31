using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    internal class ResolverCompiler<T>
        where T : IResolverContext
    {
        private readonly ParameterExpression _context;
        private readonly PropertyInfo _requestAborted;
        private readonly MethodInfo _argument;
        private readonly MethodInfo _dataLoaderWithKey;
        private readonly MethodInfo _dataLoader;
        private readonly PropertyInfo _directive;

        public ResolverCompiler()
        {
            Type contextType = typeof(T);
            TypeInfo contextTypeInfo = contextType.GetTypeInfo();

            _context = Expression.Parameter(contextType);

            _requestAborted = contextTypeInfo.GetDeclaredProperty(
                nameof(IResolverContext.RequestAborted));
            _argument = contextTypeInfo.GetDeclaredMethod(
                nameof(IResolverContext.Argument));
            _dataLoaderWithKey = typeof(DataLoaderResolverContextExtensions)
                .GetTypeInfo()
                .GetDeclaredMethod("DataLoader",
                    typeof(IResolverContext),
                    typeof(string));

            _dataLoader = typeof(DataLoaderResolverContextExtensions)
                .GetTypeInfo()
                .GetDeclaredMethod("DataLoader", typeof(IResolverContext));

            _directive = typeof(IDirectiveContext)
                .GetTypeInfo()
                .GetDeclaredProperty(nameof(IDirectiveContext.Directive));
        }

        public Func<T, object> CreateResolver(
            Expression resolverInstance,
            MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member is MethodInfo method)
            {
                IEnumerable<Expression> parameters = CreateParameters(
                    method.GetParameters());

                MethodCallExpression methodCall = Expression.Call(
                    resolverInstance, method, parameters);

                return Expression.Lambda<Func<T, object>>(
                    Expression.Convert(methodCall, typeof(object)),
                    _context).Compile();
            }

            if (member is PropertyInfo property)
            {
                MemberExpression propertyAccessor = Expression.Property(
                    resolverInstance, property);

                return Expression.Lambda<Func<T, object>>(
                    Expression.Convert(propertyAccessor, typeof(object)),
                    _context).Compile();
            }

            throw new NotSupportedException();
        }

        private IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                yield return CreateParameterExpression(parameter);
            }
        }

        private Expression CreateParameterExpression(ParameterInfo parameter)
        {
            return GetArgument(parameter);
        }

        private Expression GetDirective()
        {
            return Expression.Property(_context, _directive);
        }

        private Expression GetDataLoader(ParameterInfo parameter)
        {
            DataLoaderAttribute attribute =
                parameter.GetCustomAttribute<DataLoaderAttribute>();

            return string.IsNullOrEmpty(attribute.Key)
                ? Expression.Call(_dataLoader, _context)
                : Expression.Call(_dataLoader, _context,
                    Expression.Constant(attribute.Key));
        }

        private Expression GetCancellationToken()
        {
            return Expression.Property(_context, _requestAborted);
        }

        private Expression GetArgument(ParameterInfo parameter)
        {
            string name = parameter.IsDefined(typeof(GraphQLNameAttribute))
                ? parameter.GetCustomAttribute<GraphQLNameAttribute>().Name
                : parameter.Name;

            MethodInfo argumentMethod =
                _argument.MakeGenericMethod(parameter.ParameterType);

            return Expression.Call(_context, argumentMethod,
                Expression.Constant(new NameString(name)));
        }
    }

    internal static class TypeInfoExtensions
    {
        public static MethodInfo GetDeclaredMethod(
            this TypeInfo typeInfo,
            string name,
            params Type[] types)
        {
            return typeInfo.GetDeclaredMethods(name).FirstOrDefault(t =>
            {
                ParameterInfo[] parameters = t.GetParameters();
                if (types.Length != parameters.Length)
                {
                    return false;
                }

                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i] != parameters[i].ParameterType)
                    {
                        return false;
                    }
                }

                return true;
            });
        }
    }
}
