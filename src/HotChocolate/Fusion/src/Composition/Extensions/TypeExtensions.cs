using System.Diagnostics.CodeAnalysis;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Composition.WellKnownContextData;

namespace HotChocolate.Fusion.Composition;

internal static class TypeExtensions
{
    public static void TryApplySource<T>(
        this CompositionContext context,
        T source,
        Schema sourceSchema,
        T target)
        where T : ITypeSystemMember, IHasContextData, IHasDirectives
        => TryApplySource(context, source, sourceSchema.Name, target);

    public static void TryApplySource<T>(
        this CompositionContext context,
        T source,
        string subGraphName,
        T target)
        where T : ITypeSystemMember, IHasContextData, IHasDirectives
    {
        if (source.TryGetOriginalName(out var originalName))
        {
            target.Directives.Add(
                context.FusionTypes.CreateSourceDirective(
                    subGraphName,
                    originalName));
        }
    }

    public static void ApplySource<T>(
        this CompositionContext context,
        T source,
        Schema sourceSchema,
        T target)
        where T : ITypeSystemMember, IHasContextData, IHasDirectives
        => ApplySource(context, source, sourceSchema.Name, target);

    public static void ApplySource<T>(
        this CompositionContext context,
        T source,
        string subGraphName,
        T target)
        where T : ITypeSystemMember, IHasContextData, IHasDirectives
    {
        if (source.TryGetOriginalName(out var originalName))
        {
            target.Directives.Add(
                context.FusionTypes.CreateSourceDirective(
                    subGraphName,
                    originalName));
        }
        else
        {
            target.Directives.Add(
                context.FusionTypes.CreateSourceDirective(
                    subGraphName));
        }
    }

    public static bool TryGetOriginalName<T>(
        this T member,
        [NotNullWhen(true)] out string? originalName)
        where T : ITypeSystemMember, IHasContextData
    {
        if (member.ContextData.TryGetValue(OriginalName, out var value) &&
            value is string s)
        {
            originalName = s;
            return true;
        }

        originalName = null;
        return false;
    }

    public static string GetOriginalName<T>(this T member)
        where T : IHasName, IHasContextData
        => member.TryGetOriginalName(out var originalName)
            ? originalName
            : member.Name;
}
