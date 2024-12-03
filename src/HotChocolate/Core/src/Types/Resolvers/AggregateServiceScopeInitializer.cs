using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Resolvers;

internal sealed class AggregateServiceScopeInitializer : IServiceScopeInitializer
{
    private readonly IServiceScopeInitializer[] _initializers;

    public AggregateServiceScopeInitializer(IEnumerable<IServiceScopeInitializer> serviceScopeInitializers)
    {
        if (serviceScopeInitializers == null)
        {
            throw new ArgumentNullException(nameof(serviceScopeInitializers));
        }

        _initializers = serviceScopeInitializers.ToArray();
    }

    public void Initialize(IServiceProvider requestScope, IServiceProvider resolverScope)
    {
        switch (_initializers.Length)
        {
            case 0:
                return;

            case 1:
                _initializers[0].Initialize(requestScope, resolverScope);
                break;

            case 2:
                _initializers[0].Initialize(requestScope, resolverScope);
                _initializers[1].Initialize(requestScope, resolverScope);
                break;

            case 3:
                _initializers[0].Initialize(requestScope, resolverScope);
                _initializers[1].Initialize(requestScope, resolverScope);
                _initializers[2].Initialize(requestScope, resolverScope);
                break;

            default:
            {
                ref var start = ref MemoryMarshal.GetReference(_initializers.AsSpan());
                ref var end = ref Unsafe.Add(ref start, _initializers.Length);

                while (Unsafe.IsAddressLessThan(ref start, ref end))
                {
                    start!.Initialize(requestScope, resolverScope);
                    start = ref Unsafe.Add(ref start, 1);
                }
                break;
            }
        }
    }
}
