using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    internal class MemberResolver
        : IResolver
    {
        private readonly MemberInfo _memberInfo;
        private readonly object _fixedInstance;
        
        private ImmutableList<Func<IResolverContext, CancellationToken, object>> _parameterResolvers;
        private Func<object, IResolverContext, CancellationToken, Task<object>> _resolve;

        public MemberResolver(MemberInfo memberInfo, object fixedInstance)
        {
            _memberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));
            _fixedInstance = fixedInstance ?? throw new ArgumentNullException(nameof(fixedInstance));
        }

        public MemberResolver(MemberInfo memberInfo)
        {
            _memberInfo = memberInfo ?? throw new ArgumentNullException(nameof(memberInfo));
        }

        public async Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken)
        {
            InitializeResolver();
            return await _resolve(
                _fixedInstance ?? context.Parent<object>(),
                context, cancellationToken);
        }

        private void InitializeResolver()
        {
            if (_resolve == null)
            {
                if (_memberInfo is MethodInfo m)
                {
                    CreateParameterResolvers(m);
                    if (typeof(Task).IsAssignableFrom(m.ReturnType))
                    {
                        CreateAsyncMethodResolver(m);
                    }
                    else
                    {
                        CreateSyncMethodResolver(m);
                    }
                }

                if (_memberInfo is PropertyInfo p)
                {
                    CreatePropertyResolver(p);
                }

                if (_resolve == null)
                {
                    throw new ArgumentException("The resolver context does not provide a valid parent instance for this resolver.");
                }
            }
        }

        private void CreatePropertyResolver(PropertyInfo property)
        {
            _resolve = new Func<object, IResolverContext, CancellationToken, Task<object>>(
                (instance, context, cancellationToken) =>
                {
                    return Task.FromResult(property.GetValue(instance));
                }
            );
        }

        private void CreateSyncMethodResolver(MethodInfo method)
        {
            _resolve = new Func<object, IResolverContext, CancellationToken, Task<object>>(
                (instance, context, cancellationToken) =>
                {
                    return Task.FromResult(method.Invoke(instance, GetParameters(context, cancellationToken)));
                }
            );
        }

        private void CreateAsyncMethodResolver(MethodInfo method)
        {
            Type content = method.ReturnType.GetGenericArguments().First();
            MethodInfo methodInfo = typeof(MemberResolver)
                .GetMethod(nameof(GetMethodResult), BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(content);

            _resolve = new Func<object, IResolverContext, CancellationToken, Task<object>>(
                (instance, context, cancellationToken) =>
                {
                    return (Task<object>)methodInfo.Invoke(this, new object[]
                    {
                        method,
                        GetParameters(context, cancellationToken),
                        instance
                    });
                }
            );
        }

        private async Task<object> GetMethodResult<T>(MethodInfo method, object[] parameters, object instance)
        {
            return await (Task<T>)method.Invoke(instance, parameters);
        }

        private void CreateParameterResolvers(MethodInfo method)
        {
            // TODO: make this more modular and extendable
            if (_parameterResolvers == null)
            {
                ImmutableList<Func<IResolverContext, CancellationToken, object>> parameterResolvers
                    = ImmutableList<Func<IResolverContext, CancellationToken, object>>.Empty;

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    if (parameter.ParameterType == typeof(CancellationToken))
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => ct));
                    }
                    else if (parameter.ParameterType == typeof(IResolverContext))
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => rc));
                    }
                    else if (parameter.ParameterType == typeof(ISchema))
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => rc.Schema));
                    }
                    else if (parameter.ParameterType == typeof(ObjectTypeDefinition))
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => rc.TypeDefinition));
                    }
                    else if (parameter.ParameterType == typeof(FieldDefinition))
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => rc.FieldDefinition));
                    }
                    else if (parameter.ParameterType == typeof(QueryDocument))
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => rc.QueryDocument));
                    }
                    else if (parameter.ParameterType == typeof(OperationDefinition))
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => rc.OperationDefinition));
                    }
                    else if (parameter.ParameterType == typeof(Field))
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => rc.Field));
                    }
                    else
                    {
                        parameterResolvers = parameterResolvers.Add(
                            new Func<IResolverContext, CancellationToken, object>((rc, ct) => rc.Argument<object>(parameter.Name)));
                    }
                }
                _parameterResolvers = parameterResolvers;
            }
        }

        private object[] GetParameters(IResolverContext context, CancellationToken cancellationToken)
        {
            if (_parameterResolvers.Count == 0)
            {
                return Array.Empty<object>();
            }
            else
            {
                object[] parameters = new object[_parameterResolvers.Count];
                for (int i = 0; i < _parameterResolvers.Count; i++)
                {
                    parameters[i] = _parameterResolvers[i](context, cancellationToken);
                }
                return parameters;
            }
        }
    }
}