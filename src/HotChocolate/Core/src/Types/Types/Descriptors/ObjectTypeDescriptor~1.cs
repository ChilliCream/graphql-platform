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
    }
}
