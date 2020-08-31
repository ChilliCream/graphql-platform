#nullable enable

using System;

namespace HotChocolate.Types.Descriptors
{
    public interface ITypeReference
        : IEquatable<ITypeReference>
    {
        string? Scope { get; }

        TypeContext Context { get; }
    }
}
