using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers.Expressions.Parameters
{


    internal class ResolverCompiler<T>
        where T : IResolverContext
    {
        private readonly ParameterExpression _context;

        public ResolverCompiler()
        {
            Type contextType = typeof(T);
            TypeInfo contextTypeInfo = contextType.GetTypeInfo();

            _context = Expression.Parameter(contextType);





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
            yield break;
        }




    }
}
