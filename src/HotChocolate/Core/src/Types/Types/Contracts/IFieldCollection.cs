using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Types;

public interface IFieldCollection<out T> : IReadOnlyCollection<T> where T : class, IField
{
    T this[string fieldName] { get; }

    T this[int index] { get; }

    bool ContainsField(NameString fieldName);
}

public static class FieldCollectionExtensions
{
    public static bool TryGetField<T>(
        this IFieldCollection<T> collection,
        NameString fieldName,
        [NotNullWhen(true)] out T? field)
        where T : class, IField
    {
        if (collection is FieldCollection<T> fc)
        {
            return fc.TryGetField(fieldName, out field);
        }

        if (collection.ContainsField(fieldName))
        {
            field = collection[fieldName];
            return true;
        }

        field = default;
        return false;
    }
}
