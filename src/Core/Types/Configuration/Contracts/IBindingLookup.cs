using System.Collections.Generic;

namespace HotChocolate.Configuration
{
    internal interface IBindingLookup
    {
        IReadOnlyList<ITypeBindingInfo> Bindings { get; }

        ITypeBindingInfo GetBindingInfo(NameString typeName);
    }
}
