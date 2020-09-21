using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.RateLimit;

namespace HotChocolate.AspNetCore.RateLimit
{
    internal class RateLimitContext : IRateLimitContext
    {
        private const string ResolveAsyncMethodName = "ResolveAsync";

        private readonly IServiceProvider _serviceProvider;

        public RateLimitContext(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<RequestIdentity> CreateRequestIdentityAsync(
            IReadOnlyCollection<IPolicyIdentifier> identifiers, Path path)
        {
            string[] resolvedIdentifiers = ArrayPool<string>.Shared.Rent(identifiers.Count);

            try
            {
                for (var i = 0; i < identifiers.Count; i++)
                {
                    // Todo: build compiled expressions on startup
                    resolvedIdentifiers[i] = await ResolveAsync(identifiers.ElementAt(i));
                }

                return RequestIdentity.Create(path.ToString(), resolvedIdentifiers);
            }
            finally
            {
                ArrayPool<string>.Shared.Return(resolvedIdentifiers);
            }
        }

        private async Task<string> ResolveAsync(IPolicyIdentifier policyIdentifier)
        {
            Type policyType = policyIdentifier.GetType();
            MethodInfo[] methods = policyType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            MethodInfo[] invokeMethods = methods.Where(m =>
                    string.Equals(m.Name, ResolveAsyncMethodName, StringComparison.Ordinal))
                .ToArray();

            if (invokeMethods.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{policyType.Name} has multiple InvokeAsync method.");
            }

            if (invokeMethods.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{policyType.Name} has no InvokeAsync method.");
            }

            MethodInfo methodInfo = invokeMethods[0];
            if (!typeof(ValueTask<string>).IsAssignableFrom(methodInfo.ReturnType))
            {
                throw new InvalidOperationException(
                    $"InvokeAsync method in {policyType.Name} must have a Task<string> return type.");
            }

            object?[] invokeArgs = methodInfo
                .GetParameters()
                .Select(parameterInfo => _serviceProvider.GetService(parameterInfo.ParameterType))
                .ToArray();

            return await (ValueTask<string>)methodInfo.Invoke(policyIdentifier, invokeArgs)!;
        }
    }
}
