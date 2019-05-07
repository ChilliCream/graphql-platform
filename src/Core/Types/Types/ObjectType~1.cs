using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using NJsonSchema.Infrastructure;

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
            IInitializationContext context)
        {
            var descriptor = ObjectTypeDescriptor.New<T>(
                DescriptorContext.Create(context.Services));
            
            // Set the description before _configure so that the user can override.
            descriptor.Description(typeof(T).GetXmlSummary());
            
            _configure(descriptor);
            
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor<T> descriptor)
        {
        }

        protected sealed override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            // TODO : resources
            throw new NotSupportedException();
        }
    }
}
