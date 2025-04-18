#nullable enable

namespace HotChocolate.Types.Descriptors;

public class ObjectTypeExtensionDescriptor<T>
    : ObjectTypeDescriptorBase<T>
{
    protected internal ObjectTypeExtensionDescriptor(IDescriptorContext context)
        : base(context)
    {
        Configuration.Name = context.Naming.GetTypeName(typeof(T), TypeKind.Object);
        Configuration.Description = context.Naming.GetTypeDescription(typeof(T), TypeKind.Object);
        Configuration.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        Configuration.FieldBindingFlags = context.Options.DefaultFieldBindingFlags;
        Configuration.FieldBindingType = typeof(T);
        Configuration.IsExtension = true;
    }
}
