using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class EnumType<T>
        : EnumType
    {
        private readonly Action<IEnumTypeDescriptor<T>> _configure;

        public EnumType()
        {
            _configure = Configure;
        }

        public EnumType(Action<IEnumTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        protected override EnumTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = EnumTypeDescriptor.New<T>(
                context.DescriptorContext);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IEnumTypeDescriptor<T> descriptor)
        {
        }

        protected sealed override void Configure(IEnumTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }
    }
}
