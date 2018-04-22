using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    /*
    internal class MemberResolver
    {
        private delegate Task<object> ExecuteMemberResolverDelegate(
            IResolverContext context,
            object resolverObject,
            CancellationToken cancellationToken);

        private delegate object[] CreateParametersDelegate(
            IResolverContext context,
            ParameterInputResolver[] parameterInputResolvers,
            CancellationToken cancellationToken);

        private static readonly IParameterHandler[] _parameterHandlers =
            new IParameterHandler[]
            {
                new CancellationTokenParameterInputResolver(),
                new ResolverContextParameterInputResolver(),
                new SchemaParameterInputResolver(),
                new ObjectTypeParameterInputResolver(),
                new FieldParameterInputResolver(),
                new OperationDefinitionParameterInputResolver(),
                new FieldSelectionParameterInputResolver(),
                new ArgumentParameterInputResolver()
            };

        private readonly MemberInfo _member;
        private readonly ParameterInputResolver[] _parameterResolvers;
        private readonly ExecuteMemberResolverDelegate _executeResolver;

        public MemberResolver(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            _member = member;
            _parameterResolvers = member is MethodInfo
                ? CreateParameterInputResolvers((MethodInfo)member)
                : Array.Empty<ParameterInputResolver>();
        }

        public async Task<object> ResolveAsync(
            IResolverContext context,
            object resolverObject,
            CancellationToken cancellationToken)
        {

            return await _resolve(
                resolverObject,
                context, cancellationToken);
        }

        private static ExecuteMemberResolverDelegate CreateMemberResolver(
            MemberInfo member,
            ParameterInputResolver[] parameterInputResolvers)
        {
            if (member is MethodInfo m)
            {
                if (typeof(Task).IsAssignableFrom(m.ReturnType))
                {
                    CreateAsyncMethodResolver(m);
                }
                else
                {
                    CreateSyncMethodResolver(m);
                }
            }

            if (member is PropertyInfo p)
            {
                CreatePropertyResolver(p);
            }

            throw new ArgumentException(
                "The resolver context does not provide a valid " +
                "parent instance for this resolver.");
        }

        private static ExecuteMemberResolverDelegate CreateAsyncMethodResolver(
            MethodInfo method,
            ParameterInputResolver[] parameterInputResolvers)
        {
            Type content = method.ReturnType.GetGenericArguments().First();
            MethodInfo methodInfo = typeof(MemberResolver)
                .GetMethod(nameof(ExecuteMethodResolverAsync), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(content);

            CreateParametersDelegate createParameters;
            if (parameterInputResolvers.Length == 0)
            {
                createParameters = (ctx, p, c) => Array.Empty<object>();
            }
            else
            {
                createParameters = CreateParameters;
            }

            return new ExecuteMemberResolverDelegate(
                (context, instance, cancellationToken) =>
                {
                    object[] parameters = createParameters(
                        context, parameterInputResolvers,
                        cancellationToken);
                    return (Task<object>)methodInfo.Invoke(null, parameters);
                }
            );
        }

        private static async Task<object> ExecuteMethodResolverAsync<T>(
            MethodInfo method, object instance, object[] parameters)
        {
            return await (Task<T>)method.Invoke(instance, parameters);
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

        private void CreatePropertyResolver(PropertyInfo property)
        {
            _resolve = new Func<object, IResolverContext, CancellationToken, Task<object>>(
                (instance, context, cancellationToken) =>
                {
                    return Task.FromResult(property.GetValue(instance));
                }
            );
        }



        private async Task<object> GetMethodResult<T>(
            MethodInfo method, object[] parameters, object instance)
        {
            return await (Task<T>)method.Invoke(instance, parameters);
        }

        private static object[] CreateParameters(
            IResolverContext context,
            ParameterInputResolver[] parameterInputResolvers,
            CancellationToken cancellationToken,
            params object[] additonalParameters)
        {
            if (parameterInputResolvers.Length == 0)
            {
                return Array.Empty<object>();
            }

            object[] parameters = new object[
                parameterInputResolvers.Length + 
                additonalParameters.Length];
            
            for (int i = 0; i < additonalParameters.Length; i++)
            {
                parameters[i] = additonalParameters[i];
            }

            for (int i = additonalParameters.Length; i < parameterInputResolvers.Length; i++)
            {
                parameters[i] = parameterInputResolvers[i](
                    context, cancellationToken);
            }
            return parameters;
        }

        private static ParameterInputResolver[] CreateParameterInputResolvers(
            MethodInfo method)
        {
            ParameterInfo[] parameteres = method.GetParameters();
            ParameterInputResolver[] parameterResolvers =
                new ParameterInputResolver[parameteres.Length];

            for (int i = 0; i < parameteres.Length; i++)
            {
                ParameterInfo parameter = parameteres[i];
                for (int j = 0; i < _parameterHandlers.Length; i++)
                {
                    if (_parameterHandlers[j].TryHandle(
                        parameter, out var inputResolver))
                    {
                        parameterResolvers[i] = inputResolver;
                        break;
                    }
                }
            }

            return parameterResolvers;
        }
    }



    internal interface IParameterHandler
    {
        bool TryHandle(ParameterInfo parameter, out ParameterInputResolver inputResolver);
    }

    internal delegate object ParameterInputResolver(IResolverContext context, CancellationToken cancellationToken);


    internal sealed class CancellationTokenParameterInputResolver
        : IParameterHandler
    {
        public bool TryHandle(ParameterInfo parameter,
            out ParameterInputResolver inputResolver)
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                inputResolver = new ParameterInputResolver((rc, ct) => ct);
                return true;
            }

            inputResolver = null;
            return false;
        }
    }

    internal sealed class ResolverContextParameterInputResolver
        : IParameterHandler
    {
        public bool TryHandle(ParameterInfo parameter,
            out ParameterInputResolver inputResolver)
        {
            if (parameter.ParameterType == typeof(IResolverContext))
            {
                inputResolver = new ParameterInputResolver((rc, ct) => rc);
                return true;
            }

            inputResolver = null;
            return false;
        }
    }

    internal sealed class SchemaParameterInputResolver
       : IParameterHandler
    {
        public bool TryHandle(ParameterInfo parameter,
            out ParameterInputResolver inputResolver)
        {
            if (parameter.ParameterType == typeof(ISchema))
            {
                inputResolver = new ParameterInputResolver(
                    (rc, ct) => rc.Schema);
                return true;
            }

            inputResolver = null;
            return false;
        }
    }

    internal sealed class FieldParameterInputResolver
        : IParameterHandler
    {
        public bool TryHandle(ParameterInfo parameter,
            out ParameterInputResolver inputResolver)
        {
            if (parameter.ParameterType == typeof(Field))
            {
                inputResolver = new ParameterInputResolver(
                    (rc, ct) => rc.Field);
                return true;
            }

            inputResolver = null;
            return false;
        }
    }

    internal sealed class FieldSelectionParameterInputResolver
        : IParameterHandler
    {
        public bool TryHandle(ParameterInfo parameter,
            out ParameterInputResolver inputResolver)
        {
            if (parameter.ParameterType == typeof(ISchema))
            {
                inputResolver = new ParameterInputResolver(
                    (rc, ct) => rc.FieldSelection);
                return true;
            }

            inputResolver = null;
            return false;
        }
    }

    internal sealed class ObjectTypeParameterInputResolver
        : IParameterHandler
    {
        public bool TryHandle(ParameterInfo parameter,
            out ParameterInputResolver inputResolver)
        {
            if (parameter.ParameterType == typeof(ObjectType))
            {
                inputResolver = new ParameterInputResolver(
                    (rc, ct) => rc.ObjectType);
                return true;
            }

            inputResolver = null;
            return false;
        }
    }

    internal sealed class OperationDefinitionParameterInputResolver
        : IParameterHandler
    {
        public bool TryHandle(ParameterInfo parameter,
            out ParameterInputResolver inputResolver)
        {
            if (parameter.ParameterType == typeof(ISchema))
            {
                inputResolver = new ParameterInputResolver(
                    (rc, ct) => rc.OperationDefinition);
                return true;
            }

            inputResolver = null;
            return false;
        }
    }

    internal sealed class ArgumentParameterInputResolver
        : IParameterHandler
    {
        public bool TryHandle(ParameterInfo parameter,
            out ParameterInputResolver inputResolver)
        {
            inputResolver = new ParameterInputResolver(
                (rc, ct) => rc.Argument<object>(parameter.Name));
            return true;
        }
    }
     */
}