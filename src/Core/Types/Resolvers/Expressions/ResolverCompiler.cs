using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers.Expressions.Parameters;

namespace HotChocolate.Resolvers.Expressions
{
    internal sealed class ResolverCompiler<T>
        where T : IResolverContext
    {
        private readonly IResolverParameterCompiler[] _compilers;
        private readonly ParameterExpression _context;
        private readonly MethodInfo _taskResult;

        public ResolverCompiler(
            IEnumerable<IResolverParameterCompiler> compilers)
        {
            if (compilers == null)
            {
                throw new ArgumentNullException(nameof(compilers));
            }

            _compilers = compilers.ToArray();

            Type contextType = typeof(T);
            TypeInfo contextTypeInfo = contextType.GetTypeInfo();

            _context = Expression.Parameter(contextType);

            _taskResult = typeof(Task)
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Task.FromResult));
            _taskResult = _taskResult.MakeGenericMethod(typeof(object));
        }

        public Func<T, Task<object>> CreateResolver(
            Expression resolverInstance,
            MemberInfo member,
            Type sourceType)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member is MethodInfo method)
            {
                IEnumerable<Expression> parameters = CreateParameters(
                    method.GetParameters(), sourceType);

                MethodCallExpression methodCall = Expression.Call(
                    resolverInstance, method, parameters);

                return Expression.Lambda<Func<T, Task<object>>>(
                    Expression.Convert(methodCall, typeof(object)),
                    _context).Compile();
            }

            if (member is PropertyInfo property)
            {
                MemberExpression propertyAccessor = Expression.Property(
                    resolverInstance, property);

                return Expression.Lambda<Func<T, Task<object>>>(
                    WrapSyncResult(propertyAccessor),
                    _context).Compile();
            }

            throw new NotSupportedException();
        }

        private IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            Type sourceType)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                IResolverParameterCompiler parameterCompiler =
                    _compilers.FirstOrDefault(t =>
                        t.CanHandle(parameter, sourceType));

                if (parameterCompiler == null)
                {
                    // TODO : Resources
                    throw new InvalidOperationException(
                        "There is no default resolver parameter " +
                        "compiler available.");
                }

                yield return parameterCompiler.Compile(parameter, sourceType);
            }
        }

        private Expression WrapSyncResult(Expression result)
        {
            return Expression.Call(_taskResult,
                Expression.Convert(result, typeof(object)));
        }
    }
}
