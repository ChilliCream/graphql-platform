using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// This is not a full type and is used to split the type configuration into multiple part.
    /// Any type extension instance is will not survive the initialization and instead is
    /// merged into the target type.
    /// </summary>
    public class ObjectTypeExtension
        : NamedTypeExtensionBase<ObjectTypeDefinition>
    {
        private Action<IObjectTypeDescriptor>? _configure;

        protected ObjectTypeExtension()
        {
            _configure = Configure;
        }

        public ObjectTypeExtension(Action<IObjectTypeDescriptor> configure)
        {
            _configure = configure;
        }

        public override TypeKind Kind => TypeKind.Object;

        protected override ObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor =
                ObjectTypeDescriptor.New(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        protected override void Merge(
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is ObjectType objectType)
            {
                // we first assert that extension and type are mutable and by
                // this that they do have a type definition.
                AssertMutable();
                objectType.AssertMutable();

                ApplyGlobalFieldIgnores(
                    Definition!,
                    objectType.Definition!);

                Definition!.MergeInto(objectType.Definition!);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.ObjectTypeExtension_CannotMerge,
                    nameof(type));
            }
        }

        private void ApplyGlobalFieldIgnores(
            ObjectTypeDefinition extensionDef,
            ObjectTypeDefinition typeDef)
        {
            IReadOnlyList<ObjectFieldBinding> fieldIgnores = extensionDef.GetFieldIgnores();

            if (fieldIgnores.Count > 0)
            {
                var fields = new List<ObjectFieldDefinition>();

                foreach (ObjectFieldBinding binding in fieldIgnores)
                {
                    switch (binding.Type)
                    {
                        case ObjectFieldBindingType.Field:
                            if (typeDef.Fields.FirstOrDefault(
                                t => t.Name.Equals(binding.Name)) is { } f)
                            {
                                fields.Add(f);
                            }
                            break;

                        case ObjectFieldBindingType.Property:
                            if (typeDef.Fields.FirstOrDefault(
                                t => binding.Name.Equals(t.Member?.Name)) is { } p)
                            {
                                fields.Add(p);
                            }
                            break;
                    }
                }

                foreach (var field in fields)
                {
                    typeDef.Fields.Remove(field);
                }
            }
        }
    }
}
