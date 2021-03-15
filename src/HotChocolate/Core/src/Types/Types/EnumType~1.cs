using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class EnumType<T>
        : EnumType
        , IEnumType<T>
    {
        private Action<IEnumTypeDescriptor<T>>? _configure;

        public EnumType()
        {
            _configure = Configure;
        }

        public EnumType(Action<IEnumTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        /// <inheritdoc />
        public bool TryGetRuntimeValue(NameString name, [NotNullWhen(true)]out T runtimeValue)
        {
            if (base.TryGetRuntimeValue(name, out object? rv) &&
                rv is T casted)
            {
                runtimeValue = casted;
                return true;
            }

            runtimeValue = default!;
            return false;
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
            var descriptor =
                EnumTypeDescriptor.New<T>(context.DescriptorContext);

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }
    }
}
