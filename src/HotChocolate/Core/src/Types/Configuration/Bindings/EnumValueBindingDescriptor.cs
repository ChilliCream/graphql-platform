using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration.Bindings
{
    internal class EnumValueBindingDescriptor : IEnumValueBindingDescriptor
    {
        private readonly IDescriptorContext _context;
        private readonly IEnumTypeBindingDescriptor _typeBindingDescriptor;
        private readonly EnumValueDefinition _definition;

        public EnumValueBindingDescriptor(
            IDescriptorContext context,
            IEnumTypeBindingDescriptor typeBindingDescriptor,
            EnumValueDefinition definition)
        {
            _context = context;
            _typeBindingDescriptor = typeBindingDescriptor;
            _definition = definition;
        }

        public IEnumTypeBindingDescriptor To(NameString valueName)
        {
            _definition.Name = valueName.EnsureNotEmpty(nameof(valueName));
            return _typeBindingDescriptor;
        }
    }
}
