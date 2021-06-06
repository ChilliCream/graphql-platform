using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class DirectiveType<TDirective> : DirectiveType where TDirective : class
    {
        private Action<IDirectiveTypeDescriptor<TDirective>>? _conf;

        protected DirectiveType()
        {
            _conf = Configure;
        }

        public DirectiveType(Action<IDirectiveTypeDescriptor<TDirective>> configure)
        {
            _conf = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        protected override DirectiveTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = DirectiveTypeDescriptor.New<TDirective>(context.DescriptorContext);

            _conf!(descriptor);
            _conf = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IDirectiveTypeDescriptor<TDirective> descriptor)
        {
        }

        protected sealed override void Configure(IDirectiveTypeDescriptor descriptor)
            => throw new NotSupportedException();
    }
}
