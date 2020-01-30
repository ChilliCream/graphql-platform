using System;

namespace HotChocolate.Types.Descriptors
{
    public interface IClrTypeReference
        : ITypeReference
    {
        Type Type { get; }

        IClrTypeReference Compile();

        IClrTypeReference WithoutContext();

        IClrTypeReference WithType(Type type);
    }
}
