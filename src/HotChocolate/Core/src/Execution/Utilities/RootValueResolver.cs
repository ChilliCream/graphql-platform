using System;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
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

            if (rootType.ClrType != typeof(object))
            {
                object? rootValue = services.GetService(rootType.ClrType);

                if (rootValue is null &&
                    !rootType.ClrType.IsAbstract &&
                    !rootType.ClrType.IsInterface)
                {
                    rootValue = context.Activator.CreateInstance(rootType.ClrType);
                    cachedValue = rootValue;
                }

                return rootValue;
            }

            return null;
        }
    }
}
