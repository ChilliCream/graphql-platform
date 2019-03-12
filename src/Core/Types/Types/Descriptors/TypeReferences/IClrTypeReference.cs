using System;

namespace HotChocolate.Types.Descriptors
{
    public interface IClrTypeReference
        : ITypeReference
    {
        Type Type { get; }

        IClrTypeReference Compile();
    }
}
