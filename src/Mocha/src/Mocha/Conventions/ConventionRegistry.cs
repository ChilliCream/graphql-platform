using System.Collections;
using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Stores all registered conventions and provides efficient type-filtered access via caching.
/// </summary>
public sealed class ConventionRegistry(IEnumerable<IConvention> conventions) : IConventionRegistry
{
    private readonly ImmutableArray<IConvention> _conventions = [.. conventions];

    private ImmutableDictionary<Type, object> _conventionsByType = ImmutableDictionary<Type, object>.Empty;

    /// <inheritdoc />
    public ImmutableArray<TConvention> GetConventions<TConvention>() where TConvention : IConvention
    {
        return (ImmutableArray<TConvention>)
            ImmutableInterlocked.GetOrAdd(
                ref _conventionsByType,
                typeof(TConvention),
                static (_, conventions) =>
                {
                    var builder = ImmutableArray.CreateBuilder<TConvention>();
                    foreach (var convention in conventions.AsSpan())
                    {
                        if (convention is TConvention tConvention)
                        {
                            builder.Add(tConvention);
                        }
                    }

                    return builder.ToImmutable();
                },
                _conventions);
    }

    /// <inheritdoc />
    public IEnumerator<IConvention> GetEnumerator()
    {
        return _conventions.AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => _conventions.Length;

    /// <inheritdoc />
    public IConvention this[int index] => _conventions[index];
}
