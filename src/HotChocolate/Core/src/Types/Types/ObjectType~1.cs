using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class ObjectType<T>
        : ObjectType
    {
        private readonly Action<IObjectTypeDescriptor<T>> _configure;

        public ObjectType()
        {
            _configure = Configure;
        }

        public ObjectType(Action<IObjectTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected override ObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = ObjectTypeDescriptor.New<T>(
                context.DescriptorContext);
            _configure(descriptor);
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
