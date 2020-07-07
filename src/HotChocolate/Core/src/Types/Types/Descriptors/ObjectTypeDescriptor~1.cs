using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectTypeDescriptor<T>
        : ObjectTypeDescriptorBase<T>
    {
        public ObjectTypeDescriptor(IDescriptorContext context)
            : base(context, typeof(T))
        {
            Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        }

        public ObjectTypeDescriptor(IDescriptorContext context, ObjectTypeDefinition definition)
            : base(context, typeof(T))
        {
            Definition = definition;
        }
    }
}
