using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeExtensionDescriptor<T>
        : ObjectTypeDescriptorBase<T>
    {
        public ObjectTypeExtensionDescriptor(IDescriptorContext context)
            : base(context)
        {
            Definition.Name = context.Naming.GetTypeName(typeof(T), TypeKind.Object);
            Definition.Description = context.Naming.GetTypeDescription(typeof(T), TypeKind.Object);
            Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
            Definition.FieldBindingType = typeof(T);
        }

        protected override void OnCreateDefinition(
            ObjectTypeDefinition definition)
        {
            if (definition.FieldBindingType is { })
            {
                Context.Inspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.FieldBindingType);
            }

            base.OnCreateDefinition(definition);
        }

        protected override void OnCompleteFields(
            IDictionary<NameString, ObjectFieldDefinition> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (Definition.Fields.IsImplicitBinding())
            {
                FieldDescriptorUtilities.AddImplicitFields(
                    this,
                    Definition.FieldBindingType,
                    p =>
                    {
                        ObjectFieldDescriptor descriptor = ObjectFieldDescriptor.New(
                            Context, p, Definition.ClrType, Definition.FieldBindingType);
                        Fields.Add(descriptor);
                        return descriptor.CreateDefinition();
                    },
                    fields,
                    handledMembers);
            }

            base.OnCompleteFields(fields, handledMembers);
        }
    }
}
