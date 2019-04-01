using System;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class InputObjectType<T>
        : InputObjectType
    {
        private readonly Action<IInputObjectTypeDescriptor<T>> _configure;

        public InputObjectType()
        {
            _configure = Configure;
        }

        public InputObjectType(Action<IInputObjectTypeDescriptor<T>> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        #region Configuration

        protected override InputObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor =
                InputObjectTypeDescriptor.New<T>(
                    DescriptorContext.Create(context.Services));
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(
            IInputObjectTypeDescriptor<T> descriptor)
        {
        }

        protected sealed override void Configure(
            IInputObjectTypeDescriptor descriptor)
        {
            // TODO : resources
            throw new NotSupportedException();
        }

        #endregion
    }
}
