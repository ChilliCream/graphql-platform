using System;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Interface type extensions are used to represent an interface which has been extended
    /// from some original interface.
    ///
    /// For example, this might be used to represent common local data on many types,
    /// or by a GraphQL service which is itself an extension of another GraphQL service.
    /// </summary>
    public class InterfaceTypeExtension : NamedTypeExtensionBase<InterfaceTypeDefinition>
    {
        private Action<IInterfaceTypeDescriptor>? _configure;

        /// <summary>
        /// Initializes a new  instance of <see cref="InterfaceTypeExtension"/>.
        /// </summary>
        protected InterfaceTypeExtension()
        {
            _configure = Configure;
        }

        /// <summary>
        /// Initializes a new  instance of <see cref="InterfaceTypeExtension"/>.
        /// </summary>
        /// <param name="configure">
        /// A delegate to specify the properties of this type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <c>null</c>.
        /// </exception>
        public InterfaceTypeExtension(Action<IInterfaceTypeDescriptor> configure)
        {
            _configure = configure;
        }

        /// <summary>
        /// Create an object type from a type definition.
        /// </summary>
        /// <param name="definition">
        /// The object type definition that specifies the properties of the
        /// newly created object type.
        /// </param>
        /// <returns>
        /// Returns the newly created object type.
        /// </returns>
        public static InterfaceTypeExtension CreateUnsafe(InterfaceTypeDefinition definition)
            => new() { Definition = definition };

        /// <inheritdoc />
        public override TypeKind Kind => TypeKind.Interface;

        protected override InterfaceTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor =
                InterfaceTypeDescriptor.New(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        protected override void Merge(
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is InterfaceType interfaceType)
            {
                // we first assert that extension and type are mutable and by
                // this that they do have a type definition.
                AssertMutable();
                interfaceType.AssertMutable();

                TypeExtensionHelper.MergeContextData(
                    Definition!,
                    interfaceType.Definition!);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition!.Directives,
                    interfaceType.Definition!.Directives);

                TypeExtensionHelper.MergeInterfaceFields(
                    context,
                    Definition!.Fields,
                    interfaceType.Definition!.Fields);

                TypeExtensionHelper.MergeConfigurations(
                    Definition!.Configurations,
                    interfaceType.Definition!.Configurations);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.InterfaceTypeExtension_CannotMerge,
                    nameof(type));
            }
        }
    }
}
