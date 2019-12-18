using System;

#nullable enable

namespace HotChocolate.Types
{
    public interface IHasTypeIdentity
    {
        Type? TypeIdentity { get; }
    }
}
