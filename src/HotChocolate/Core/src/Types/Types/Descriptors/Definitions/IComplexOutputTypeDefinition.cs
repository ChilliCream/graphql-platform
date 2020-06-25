using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface IComplexOutputTypeDefinition
    {
        Type RuntimeType { get; }

        IList<Type> KnownClrTypes { get; }

        IList<ITypeReference> Interfaces { get; }
    }
}
