using System.Collections.Generic;
using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq;

namespace HotChocolate.Types
{
    public class ObjectTypeExtension
        : NamedTypeExtensionBase<ObjectTypeDefinition>
    {
        private readonly Action<IObjectTypeDescriptor> _configure;

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
            IInitializationContext context)
        {
            var descriptor = ObjectTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        internal override void Merge(INamedType type)
        {
            if (type is ObjectType objectType)
            {
                ObjectTypeDefinition typeDefinition = objectType.Definition;

                foreach (KeyValuePair<string, object> item in
                    Definition.ContextData)
                {
                    typeDefinition.ContextData[item.Key] = item.Value;
                }









            }

            // TODO : resources
            throw new ArgumentException("CANNOT MERGE");
        }

        private void MergeFields(ObjectTypeDefinition typeDefinition)
        {
            foreach (ObjectFieldDefinition field in Definition.Fields)
            {
                ObjectFieldDefinition current = typeDefinition.Fields
                    .FirstOrDefault(t => t.Name.Equals(field.Name));

                if (current == null)
                {
                    typeDefinition.Fields.Add(field);
                }
                else
                {

                }
            }
        }
    }
}
