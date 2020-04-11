﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types
{
    public interface IFieldCollection<out T>
        : IReadOnlyCollection<T>
        where T : IField
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
            [NotNullWhen(true)]out T field)
            where T : IField
        {
            if (collection.ContainsField(fieldName))
            {
                field = collection[fieldName];
                return true;
            }

            field = default;
            return false;
        }
    }
}
