using HotChocolate.Fusion.SourceSchema.Types;

namespace HotChocolate.Types;

public static class FusionObjectFieldDescriptorExtensions
{
    public static IObjectFieldDescriptor Internal(
        this IObjectFieldDescriptor descriptor)
        => descriptor.Directive(InternalDirective.Instance);

    public static IObjectFieldDescriptor Lookup(
        this IObjectFieldDescriptor descriptor)
        => descriptor.Directive(LookupDirective.Instance);

    public static IArgumentDescriptor Is(
        this IArgumentDescriptor descriptor,
        string field)
        => descriptor.Directive(new IsDirective(field));
}
