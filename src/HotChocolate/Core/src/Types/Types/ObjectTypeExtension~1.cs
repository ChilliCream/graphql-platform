using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectTypeExtension<T>
        : ObjectTypeExtension
    {
        private Action<IObjectTypeDescriptor<T>>? _configure;

        public ObjectTypeExtension()
        {
            _configure = Configure;
        }

        public ObjectTypeExtension(Action<IObjectTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected override ObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            ObjectTypeExtensionDescriptor<T> descriptor =
                ObjectTypeDescriptor.NewExtension<T>(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor<T> descriptor)
        {
        }

        protected sealed override void Configure(IObjectTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }
    }
}
