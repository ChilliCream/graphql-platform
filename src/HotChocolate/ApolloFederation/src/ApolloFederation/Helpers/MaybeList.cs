using System.Collections.Generic;

namespace HotChocolate.ApolloFederation.Helpers;

internal struct MaybeList<T>
{
    public MaybeList(List<T>? list)
    {
        _list = list;
    }

    private List<T>? _list;
    public List<T> GetOrCreateList() => _list ??= new();
    public readonly IReadOnlyList<T> ReadOnlyList
    {
        get
        {
            if (_list is not null)
            {
                return _list;
            }
            return Array.Empty<T>();
        }
    }
}
