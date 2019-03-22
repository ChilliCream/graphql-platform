using HotChocolate.Configuration.Bindings;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    internal interface IBindingCompiler
    {
        bool CanHandle(IBindingInfo binding);

        void AddBinding(IBindingInfo binding);

        IBindingLookup Compile(IDescriptorContext descriptorContext);
    }
}
