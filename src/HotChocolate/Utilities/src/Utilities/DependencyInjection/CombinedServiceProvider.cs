using System.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Utilities;

internal sealed class CombinedServiceProvider : IServiceProvider
{
    private static List<object>? _buffer = [];
    private static readonly Type _enumerable = typeof(IEnumerable<>);
    private readonly IServiceProvider _first;
    private readonly IServiceProvider _second;
    private readonly IServiceProviderIsService? _serviceInspector;

    public CombinedServiceProvider(IServiceProvider first, IServiceProvider second)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _second = second ?? throw new ArgumentNullException(nameof(second));

        var firstInspector = _first.GetService<IServiceProviderIsService>();
        var secondInspector = _second.GetService<IServiceProviderIsService>();

        if (firstInspector is not null && secondInspector is not null)
        {
            _serviceInspector = new CombinedServiceProviderIsService(firstInspector, secondInspector);
        }
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType is null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (serviceType == typeof(IServiceProviderIsService))
        {
            return _serviceInspector;
        }

        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == _enumerable)
        {
            var elementType = serviceType.GetGenericArguments()[0];
            var firstResult = (IEnumerable?)_first.GetService(serviceType);
            var secondResult = (IEnumerable?)_second.GetService(serviceType);
            return Concat(elementType, firstResult, secondResult);
        }

        return _first.GetService(serviceType) ?? _second.GetService(serviceType);
    }

    private static object? Concat(
        Type elementType,
        IEnumerable? servicesFromA,
        IEnumerable? servicesFromB)
    {
        if (servicesFromA is null)
        {
            return servicesFromB;
        }

        if (servicesFromB is null)
        {
            return servicesFromA;
        }

        var enumeratorA = servicesFromA.GetEnumerator();
        var disposableA = enumeratorA as IDisposable;
        var enumeratorB = servicesFromB.GetEnumerator();
        var disposableB = enumeratorB as IDisposable;

        try
        {
            var buffer = Interlocked.Exchange(ref _buffer, null) ?? [];

            while (enumeratorA.MoveNext())
            {
                if (enumeratorA.Current is not null)
                {
                    buffer.Add(enumeratorA.Current);
                }
            }

            while (enumeratorB.MoveNext())
            {
                if (enumeratorB.Current is not null)
                {
                    buffer.Add(enumeratorB.Current);
                }
            }

            var array = Array.CreateInstance(elementType, buffer.Count);

            for (var i = 0; i < buffer.Count; i++)
            {
                array.SetValue(buffer[i], i);
            }

            buffer.Clear();
            Interlocked.CompareExchange(ref buffer, buffer, null);

            return array;
        }
        finally
        {
            disposableA?.Dispose();
            disposableB?.Dispose();
        }
    }

    private sealed class CombinedServiceProviderIsService(
        IServiceProviderIsService first,
        IServiceProviderIsService second)
        : IServiceProviderIsService
    {
        public bool IsService(Type serviceType)
            => first.IsService(serviceType) ||
                second.IsService(serviceType);
    }
}
