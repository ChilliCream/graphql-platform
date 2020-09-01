using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class EnumType<T>
        : EnumType
        , IEnumType<T>
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

        public IReadOnlyCollection<IEnumValue<T>> Values =>
            (IReadOnlyCollection<IEnumValue<T>>)base.Values;

        public bool TryGetRuntimeValue(NameString name, out T runtimeValue)
        {
            throw new NotImplementedException();
        }

        protected virtual void Configure(IEnumTypeDescriptor<T> descriptor)
        {
        }

        protected sealed override void Configure(IEnumTypeDescriptor descriptor)
        {
            throw new NotSupportedException();
        }

        protected override EnumTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = EnumTypeDescriptor.New<T>(
                context.DescriptorContext);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }
    }
}
