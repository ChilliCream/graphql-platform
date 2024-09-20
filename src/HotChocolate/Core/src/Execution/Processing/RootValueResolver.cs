using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.Properties.Resources;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// This helper will resolve the initial value for the execution engine.
/// </summary>
internal static class RootValueResolver
{
    public static object? Resolve(
        IRequestContext context,
        IServiceProvider services,
        ObjectType rootType,
        ref object? cachedValue)
    {
        if (context.ContextData.TryGetValue(WellKnownContextData.InitialValue, out var o) &&
            o is not null)
        {
            return o;
        }

        // if the initial value is a singleton and was already resolved,
        // we will use this instance.
        if (cachedValue is not null)
        {
            return cachedValue;
        }

        // if the operation type has a proper runtime representation we will try to resolve
        // that from the request services.
        if (rootType.RuntimeType != typeof(object))
        {
            var rootValue = services.GetService(rootType.RuntimeType);

            // if the request services did not provide a rootValue and the runtime
            // representation is an instantiable class we will create a singleton ourselves
            // and store it as cached value in order to reuse it.
            if (rootValue is null &&
                !rootType.RuntimeType.IsAbstract &&
                !rootType.RuntimeType.IsInterface)
            {
                try
                {
                    rootValue = ActivatorUtilities.CreateInstance(services, rootType.RuntimeType);
                    cachedValue = rootValue;
                }
                catch (Exception ex)
                {
                    throw new GraphQLException(
                        ErrorBuilder.New()
                            .SetMessage(
                                RootValueResolver_Resolve_CannotCreateInstance,
                                rootType.Name,
                                rootType.RuntimeType.FullName ?? rootType.RuntimeType.Name)
                            .SetCode(ErrorCodes.Execution.CannotCreateRootValue)
                            .SetExtension("operationType", rootType.Name)
                            .SetExtension("runtimeType", rootType.RuntimeType.FullName)
                            .SetException(ex)
                            .Build());
                }
            }

            return rootValue;
        }

        return null;
    }
}
