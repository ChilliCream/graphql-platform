using System;

namespace HotChocolate.Configuration
{
    public interface ITypeBinding
    {
        string Name { get; }

        Type Type { get; }
    }
}
