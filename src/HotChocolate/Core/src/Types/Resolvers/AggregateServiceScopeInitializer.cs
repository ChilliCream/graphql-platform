using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Resolvers;

internal sealed class AggregateServiceScopeInitializer : IServiceScopeInitializer
{
    private readonly IServiceScopeInitializer[] _initializers;

    public AggregateServiceScopeInitializer(IEnumerable<IServiceScopeInitializer> serviceScopeInitializers)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeInitializers);

        _initializers = serviceScopeInitializers.ToArray();
    }

    public void Initialize(
        IMiddlewareContext context,
        IServiceProvider requestScope,
        IServiceProvider resolverScope)
    {
        switch (_initializers.Length)
        {
            case 0:
                return;

            case 1:
                _initializers[0].Initialize(context, requestScope, resolverScope);
                break;

            case 2:
                _initializers[0].Initialize(context, requestScope, resolverScope);
                _initializers[1].Initialize(context, requestScope, resolverScope);
                break;

            case 3:
                _initializers[0].Initialize(context, requestScope, resolverScope);
                _initializers[1].Initialize(context, requestScope, resolverScope);
                _initializers[2].Initialize(context, requestScope, resolverScope);
                break;

            default:
                ref var start = ref MemoryMarshal.GetReference(_initializers.AsSpan());
                ref var end = ref Unsafe.Add(ref start, _initializers.Length);

                while (Unsafe.IsAddressLessThan(ref start, ref end))
                {
                    start!.Initialize(context, requestScope, resolverScope);
                    start = ref Unsafe.Add(ref start, 1);
                }
                break;
        }
    }
}
