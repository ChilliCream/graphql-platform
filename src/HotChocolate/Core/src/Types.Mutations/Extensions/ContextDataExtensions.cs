using HotChocolate.Features;

namespace HotChocolate.Types;

internal static class ContextDataExtensions
{
    public static List<MutationContextData> GetMutationFields(
        this ITypeCompletionContext context)
        => context.Features.GetOrSet<List<MutationContextData>>();

    public static List<MutationContextData> GetMutationFields(
        this IDescriptorContext context)
        => context.Features.GetOrSet<List<MutationContextData>>();
}
