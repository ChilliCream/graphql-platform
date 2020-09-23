using System;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal static class RootValueResolver
    {
        public static object? TryResolve(
            IRequestContext context,
            IServiceProvider services,
            ObjectType rootType,
            ref object? cachedValue)
        {
            if (context.Request.InitialValue is { })
            {
                return context.Request.InitialValue;
            }

            if (cachedValue is { })
            {
                return cachedValue;
            }

            if (rootType.RuntimeType != typeof(object))
            {
                object? rootValue = services.GetService(rootType.RuntimeType);

                if (rootValue is null &&
                    !rootType.RuntimeType.IsAbstract &&
                    !rootType.RuntimeType.IsInterface)
                {
                    rootValue = context.Activator.CreateInstance(rootType.RuntimeType);
                    cachedValue = rootValue;
                }

                return rootValue;
            }

            return null;
        }
    }
}
