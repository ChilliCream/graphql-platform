using System;

namespace HotChocolate.Configuration
{
    public interface ITypeBinding
    {
        NameString Name { get; }

        Type Type { get; }
    }
}
