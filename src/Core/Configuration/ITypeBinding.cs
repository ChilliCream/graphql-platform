using System;

namespace HotChocolate.Configuration
{
    internal interface ITypeBinding
    {
        string Name { get; }
        Type Type { get; }
    }
}
