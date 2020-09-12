#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeExtensionDescriptor<T>
        : ObjectTypeDescriptorBase<T>
    {
        protected internal ObjectTypeExtensionDescriptor(IDescriptorContext context)
            : base(context)
        {
            Definition.Name = context.Naming.GetTypeName(typeof(T), TypeKind.Object);
            Definition.Description = context.Naming.GetTypeDescription(typeof(T), TypeKind.Object);
            Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
            Definition.FieldBindingType = typeof(T);
        }
    }
}
