using System;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface IClrTypeReference
        : ITypeReference
    {
        TypeContext Context { get; }

        Type Type { get; }
    }
}
