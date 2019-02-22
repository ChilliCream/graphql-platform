using System;

namespace HotChocolate.Types.Descriptors
{
    public interface IClrTypeReference
        : ITypeReference
    {
        TypeContext Context { get; }

        Type Type { get; }
    }
}
