using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal static class TypeExtensions
{
    public static void TryApplySource<T>(
        this CompositionContext context,
        T source,
        SchemaDefinition sourceSchema,
        T target)
        where T : ITypeSystemMemberDefinition, IFeatureProvider, IDirectivesProvider
        => TryApplySource(context, source, sourceSchema.Name, target);

    public static void TryApplySource<T>(
        this CompositionContext context,
        T source,
        string subgraphName,
        T target)
        where T : ITypeSystemMemberDefinition, IFeatureProvider, IDirectivesProvider
    {
        if (target.ContainsInternalDirective())
        {
            return;
        }

        if (source.TryGetOriginalName(out var originalName))
        {
            target.Directives.Add(
                context.FusionTypes.CreateSourceDirective(
                    subgraphName,
                    originalName));
        }
    }

    public static void ApplySource<T>(
        this CompositionContext context,
        T source,
        SchemaDefinition sourceSchema,
        T target)
        where T : ITypeSystemMemberDefinition, IFeatureProvider, IDirectivesProvider
        => ApplySource(context, source, sourceSchema.Name, target);

    public static void ApplySource<T>(
        this CompositionContext context,
        T source,
        string subgraphName,
        T target)
        where T : ITypeSystemMemberDefinition, IFeatureProvider, IDirectivesProvider
    {
        if (target.ContainsInternalDirective())
        {
            return;
        }

        if (source.TryGetOriginalName(out var originalName))
        {
            target.Directives.Add(
                context.FusionTypes.CreateSourceDirective(
                    subgraphName,
                    originalName));
        }
        else
        {
            target.Directives.Add(
                context.FusionTypes.CreateSourceDirective(
                    subgraphName));
        }
    }

    public static bool TryGetOriginalName<T>(
        this T member,
        [NotNullWhen(true)] out string? originalName)
        where T : ITypeSystemMemberDefinition, IFeatureProvider
    {
        var metadata = member.Features.Get<FusionMemberMetadata>();

        if(metadata?.OriginalName is null)
        {
            originalName = null;
            return false;
        }

        originalName = metadata.OriginalName;
        return true;
    }

    public static string GetOriginalName<T>(this T member)
        where T : INameProvider, IFeatureProvider
        => member.TryGetOriginalName(out var originalName)
            ? originalName
            : member.Name;
}
