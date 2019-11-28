using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Utilities
{
    public interface IExtendedType
    {
        Type Type { get; }

        Type? Definition { get; }

        ExtendedTypeKind Kind { get; }

        bool IsGeneric { get; }

        bool IsArray { get; }

        bool IsInterface { get; }

        bool IsNullable { get; }

        IReadOnlyList<IExtendedType> TypeArguments { get; }

        IReadOnlyList<IExtendedType> GetInterfaces();

        IExtendedType WithIsNullable(bool isNullable);

        IExtendedType WithTypeArguments(IReadOnlyList<IExtendedType> arguments);
    }
}
