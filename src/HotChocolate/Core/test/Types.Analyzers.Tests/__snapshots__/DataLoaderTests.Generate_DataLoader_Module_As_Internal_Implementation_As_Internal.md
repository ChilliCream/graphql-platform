# Generate_DataLoader_Module_As_Internal_Implementation_As_Internal

## GreenDonutDataLoader.735550c.g.cs

```csharp
// <auto-generated/>

#nullable enable
#pragma warning disable

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using GreenDonut;

namespace TestNamespace
{
    public interface IEntityByIdDataLoader
        : global::GreenDonut.IDataLoader<int, string>
    {
    }

    internal sealed partial class EntityByIdDataLoader
        : global::GreenDonut.DataLoaderBase<int, string>
        , IEntityByIdDataLoader
    {
        private readonly global::System.IServiceProvider _services;

        public EntityByIdDataLoader(
            global::System.IServiceProvider services,
            global::GreenDonut.IBatchScheduler batchScheduler,
            global::GreenDonut.DataLoaderOptions options)
            : base(batchScheduler, options)
        {
            _services = services ??
                throw new global::System.ArgumentNullException(nameof(services));
        }

        protected override async global::System.Threading.Tasks.ValueTask FetchAsync(
            global::System.Collections.Generic.IReadOnlyList<int> keys,
            global::System.Memory<GreenDonut.Result<string?>> results,
            global::GreenDonut.DataLoaderFetchContext<string> context,
            global::System.Threading.CancellationToken ct)
        {
            var p1 = context.GetState<global::GreenDonut.Data.IPredicateBuilder>("GreenDonut.Data.Predicate")
                ?? global::GreenDonut.Data.DefaultPredicateBuilder.Empty;
            var temp = await global::TestNamespace.TestClass.GetEntityByIdAsync(keys, p1, ct).ConfigureAwait(false);
            CopyResults(keys, results.Span, temp);
        }

        private void CopyResults(
            global::System.Collections.Generic.IReadOnlyList<int> keys,
            global::System.Span<GreenDonut.Result<string?>> results,
            global::System.Collections.Generic.IDictionary<int, string> resultMap)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (resultMap.TryGetValue(key, out var value))
                {
                    results[i] = global::GreenDonut.Result<string?>.Resolve(value);
                }
                else
                {
                    results[i] = global::GreenDonut.Result<string?>.Resolve(default(string));
                }
            }
        }
    }
}


```

## GreenDonutDataLoaderModule.735550c.g.cs

```csharp
// <auto-generated/>

#nullable enable
#pragma warning disable

using System;
using System.Runtime.CompilerServices;
using GreenDonut;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static partial class AbcDataLoaderServiceExtensions
    {
        public static IServiceCollection AddAbc(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            global::Microsoft.Extensions.DependencyInjection.DataLoaderServiceCollectionExtensions.AddDataLoader<global::TestNamespace.IEntityByIdDataLoader, global::TestNamespace.EntityByIdDataLoader>(services);
            return services;
        }
    }
}

```

