# GenerateSource_RequestMiddleware_MatchesSnapshot

```csharp
// <auto-generated/>

#nullable enable
#pragma warning disable

using System;
using System.Runtime.CompilerServices;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Generated
{
    public static class TestsTypesMiddlewareFactoriesHASH
    {
        // global::SomeRequestMiddleware
        private static global::HotChocolate.Execution.RequestCoreMiddleware CreateMiddleware0()
            => (core, next) =>
                {
                    var cp1 = core.SchemaServices.GetRequiredService<global::Service1>();
                    var cp2 = core.SchemaServices.GetService<global::Service2>();
                    var middleware = new global::SomeRequestMiddleware(next, cp1, cp2);
                    return async context =>
                    {
                        var ip1 = context.Services.GetRequiredService<global::Service1>();
                        var ip2 = context.Services.GetService<global::Service2>();
                        await middleware.InvokeAsync(context, ip1, ip2).ConfigureAwait(false);
                    };
                };

        [InterceptsLocation("", 15, 14)]
        public static global::HotChocolate.Execution.Configuration.IRequestExecutorBuilder UseRequestGen0<TMiddleware>(
            this HotChocolate.Execution.Configuration.IRequestExecutorBuilder builder,
            string? key = null)
            where TMiddleware : class
            => builder.UseRequest(CreateMiddleware0(), key);
    }
}

#pragma warning disable CS9113 // Parameter is unread.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    file sealed class InterceptsLocationAttribute(string filePath, int line, int column) : Attribute;
}
#pragma warning restore CS9113 // Parameter is unread.
```
