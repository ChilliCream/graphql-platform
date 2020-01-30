using System.Collections.Generic;

namespace HotChocolate.Configuration
{
    internal interface IBindingLookup
    {
        IReadOnlyCollection<ITypeBindingInfo> Bindings { get; }

        ITypeBindingInfo GetBindingInfo(NameString typeName);
    }
}
