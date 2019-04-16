using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class UnionTypeExtension
        : NamedTypeExtensionBase<UnionTypeDefinition>
    {
        private readonly Action<IUnionTypeDescriptor> _configure;

        public UnionTypeExtension()
        {
            _configure = Configure;
        }

        public UnionTypeExtension(Action<IUnionTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.Union;

        protected override UnionTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = UnionTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IUnionTypeDescriptor descriptor) { }

        internal override void Merge(
            ICompletionContext context,
            INamedType type)
        {
            if (type is UnionType unionType)
            {
                TypeExtensionHelper.MergeContextData(
                    Definition,
                    unionType.Definition);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition.Directives,
                    unionType.Definition.Directives);

                TypeExtensionHelper.MergeTypes(
                    Definition.Types,
                    unionType.Definition.Types);
            }
            else
            {
                // TODO : resources
                throw new ArgumentException("CANNOT MERGE");
            }
        }
    }
}
