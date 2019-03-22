using System;

namespace HotChocolate.Configuration.Bindings
{
    public interface ITypeBinding
    {
        NameString Name { get; }

        Type Type { get; }
    }
}
