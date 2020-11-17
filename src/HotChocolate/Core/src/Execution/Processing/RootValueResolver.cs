using System;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
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
            // if an initial value was passed in with the request by the user, we will use that
            // as root value on which the execution engine starts executing.
            if (context.Request.InitialValue is not null)
            {
                return context.Request.InitialValue;
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
                object? rootValue = services.GetService(rootType.RuntimeType);

                // if the request services did not provide a rootValue and the runtime 
                // representation is a instantiatable class we will create a singleton ourselfs
                // and store it as cached value in order to reuse it.
                if (rootValue is null &&
                    !rootType.RuntimeType.IsAbstract &&
                    !rootType.RuntimeType.IsInterface)
                {
                    try
                    {
                        rootValue = context.Activator.CreateInstance(rootType.RuntimeType);
                        cachedValue = rootValue;
                    }
                    catch
                    {
                        throw new GraphQLException(
                            ErrorBuilder.New()
                                .SetMessage("Unable to create the initial value for the execution from the specified operation type `{0}` with the runtime type `{1}`. If this error happens consider registering `{1}` with the dependency injection provider manually.")
                                .SetCode(ErrorCodes.Execution.CannotCreateRootValue)
                                .SetExtension("operationType", rootType.Name)
                                .SetExtension("runtimeType", rootType.RuntimeType.FullName)
                                .Build());
                    }
                }

                return rootValue;
            }

            return null;
        }
    }
}
