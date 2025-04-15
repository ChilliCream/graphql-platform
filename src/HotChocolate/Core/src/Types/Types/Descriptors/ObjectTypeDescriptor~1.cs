using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors;

public class ObjectTypeDescriptor<T> : ObjectTypeDescriptorBase<T>
{
    protected internal ObjectTypeDescriptor(IDescriptorContext context)
        : base(context, typeof(T))
    {
        Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        Definition.FieldBindingFlags = context.Options.DefaultFieldBindingFlags;
    }

    protected internal ObjectTypeDescriptor(
        IDescriptorContext context,
        ObjectTypeConfiguration definition)
        : base(context, definition)
    {
        Definition = definition;
    }
}
