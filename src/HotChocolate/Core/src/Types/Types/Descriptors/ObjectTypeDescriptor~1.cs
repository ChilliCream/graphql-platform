using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeDescriptor<T>
        : ObjectTypeDescriptorBase<T>
    {
        protected internal ObjectTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
            Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        }

        protected internal ObjectTypeDescriptor(
            IDescriptorContext context, 
            ObjectTypeDefinition definition)
            : base(context, definition)
        {
            Definition = definition;
        }
    }
}
