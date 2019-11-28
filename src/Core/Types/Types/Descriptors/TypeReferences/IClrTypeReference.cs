using System;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public interface IClrTypeReference
        : ITypeReference
    {
        IExtendedType Type { get; }

        IClrTypeReference Compile();

        IClrTypeReference WithoutContext();

        IClrTypeReference WithType(IExtendedType type);
    }
}
