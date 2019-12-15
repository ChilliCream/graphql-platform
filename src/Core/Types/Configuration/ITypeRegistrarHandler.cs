using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    internal interface ITypeRegistrarHandler
    {
        void Register(
            ITypeRegistrar typeRegistrar,
            IEnumerable<ITypeReference> typeReferences);
    }
}
