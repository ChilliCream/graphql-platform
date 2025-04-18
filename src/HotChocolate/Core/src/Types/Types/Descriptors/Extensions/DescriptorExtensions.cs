using HotChocolate.Types.Descriptors.Definitions;

// we put this in the descriptor namespace so that not everyone get these by default.
namespace HotChocolate.Types.Descriptors;

public static class DescriptorExtensions
{
    public static ObjectTypeDescriptor ToDescriptor(
        this ObjectTypeConfiguration definition,
        IDescriptorContext context)
        => ObjectTypeDescriptor.From(context, definition);

    public static ObjectFieldDescriptor ToDescriptor(
        this ObjectFieldConfiguration definition,
        IDescriptorContext context)
        => ObjectFieldDescriptor.From(context, definition);

    public static ArgumentDescriptor ToDescriptor(
        this ArgumentConfiguration definition,
        IDescriptorContext context)
        => ArgumentDescriptor.From(context, definition);

    public static EnumTypeDescriptor ToDescriptor(
        this EnumTypeConfiguration definition,
        IDescriptorContext context)
        => EnumTypeDescriptor.From(context, definition);

    public static EnumValueDescriptor ToDescriptor(
        this EnumValueConfiguration definition,
        IDescriptorContext context)
        => EnumValueDescriptor.From(context, definition);

    public static T ToDefinition<T>(this IDescriptor<T> descriptor) where T : TypeSystemConfiguration
        => descriptor is DescriptorBase<T> desc
            ? desc.CreateConfiguration()
            : throw new NotSupportedException("The specified descriptor is not supported.");
}
