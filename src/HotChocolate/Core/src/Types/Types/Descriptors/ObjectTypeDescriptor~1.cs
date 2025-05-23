using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Descriptors;

public class ObjectTypeDescriptor<T> : ObjectTypeDescriptorBase<T>
{
    protected internal ObjectTypeDescriptor(IDescriptorContext context)
        : base(context, typeof(T))
    {
        Configuration.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        Configuration.FieldBindingFlags = context.Options.DefaultFieldBindingFlags;
    }

    protected internal ObjectTypeDescriptor(
        IDescriptorContext context,
        ObjectTypeConfiguration definition)
        : base(context, definition)
    {
        Configuration = definition;
    }
}
