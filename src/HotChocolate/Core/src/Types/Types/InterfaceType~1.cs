using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class InterfaceType<T> : InterfaceType
    {
        private Action<IInterfaceTypeDescriptor<T>>? _configure;

        public InterfaceType() =>
            _configure = Configure;

        public InterfaceType(Action<IInterfaceTypeDescriptor<T>> configure) =>
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));

        protected override InterfaceTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            var descriptor = InterfaceTypeDescriptor.New<T>(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInterfaceTypeDescriptor<T> descriptor) { }

        protected sealed override void Configure(IInterfaceTypeDescriptor descriptor) =>
            throw new NotSupportedException();
    }
}
