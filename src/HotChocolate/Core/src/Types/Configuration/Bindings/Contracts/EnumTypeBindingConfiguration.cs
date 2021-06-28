using System;

namespace HotChocolate.Configuration.Bindings
{
    internal class EnumTypeBindingConfiguration
    {
        public EnumTypeBindingConfiguration(
            Type runtimeType,
            Action<IEnumTypeBindingDescriptor> configure)
        {
            RuntimeType = runtimeType ?? throw new ArgumentNullException(nameof(runtimeType));
            Configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public Type RuntimeType { get; }

        public Action<IEnumTypeBindingDescriptor> Configure { get; }
    }
}
