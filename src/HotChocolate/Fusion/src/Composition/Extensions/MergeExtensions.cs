using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

internal static class MergeExtensions
{
    internal static ITypeDefinition? MergeOutputType(ITypeDefinition source, ITypeDefinition target)
    {
        if (source.Equals(target, TypeComparison.Structural))
        {
            return target;
        }

        if (target.Kind is TypeKind.NonNull && source.Kind is not TypeKind.NonNull)
        {
            var nullableTarget = target.InnerType();

            if (source.Equals(nullableTarget, TypeComparison.Structural))
            {
                return nullableTarget;
            }

            if (source.Kind == nullableTarget.Kind && nullableTarget.Kind == TypeKind.List)
            {
                var rewrittenType = MergeOutputType(source.InnerType(), nullableTarget.InnerType());

                if (rewrittenType is not null)
                {
                    return new ListTypeDefinition(rewrittenType);
                }
            }

            return null;
        }

        if (target.Kind is not TypeKind.NonNull && source.Kind is TypeKind.NonNull)
        {
            var nullableSource = source.InnerType();

            if (nullableSource.Equals(target, TypeComparison.Structural))
            {
                return target;
            }

            if (nullableSource.Kind == target.Kind && target.Kind == TypeKind.List)
            {
                var rewrittenType = MergeOutputType(nullableSource.InnerType(), target.InnerType());

                if (rewrittenType is not null)
                {
                    return new ListTypeDefinition(rewrittenType);
                }
            }

            return null;
        }

        if (source.Kind == target.Kind && target.IsListType() && source.IsListType())
        {
            if (source.Kind is TypeKind.NonNull)
            {
                var rewrittenType = MergeOutputType(source.InnerType().InnerType(), target.InnerType().InnerType());

                if (rewrittenType is not null)
                {
                    return new NonNullTypeDefinition(new ListTypeDefinition(rewrittenType));
                }
            }
            else
            {
                var rewrittenType = MergeOutputType(source.InnerType(), target.InnerType());

                if (rewrittenType is not null)
                {
                    return new ListTypeDefinition(rewrittenType);
                }
            }
        }

        return null;
    }

    internal static ITypeDefinition? MergeInputType(ITypeDefinition source, ITypeDefinition target)
    {
        if (source.Equals(target, TypeComparison.Structural))
        {
            return target;
        }

        if (target.Kind is not TypeKind.NonNull && source.Kind is TypeKind.NonNull)
        {
            var nullableSource = source.InnerType();

            if (nullableSource.Equals(target, TypeComparison.Structural))
            {
                return new NonNullTypeDefinition(nullableSource);
            }

            if (nullableSource.Kind == target.Kind && nullableSource.Kind == TypeKind.List)
            {
                var rewrittenType = MergeInputType(nullableSource.InnerType(), target.InnerType());

                if (rewrittenType is not null)
                {
                    return new NonNullTypeDefinition(new ListTypeDefinition(rewrittenType));
                }
            }

            return null;
        }

        if (target.Kind is TypeKind.NonNull && source.Kind is not TypeKind.NonNull)
        {
            var nullableTarget = target.InnerType();

            if (source.Equals(nullableTarget, TypeComparison.Structural))
            {
                return target;
            }

            if (source.Kind == nullableTarget.Kind && nullableTarget.Kind == TypeKind.List)
            {
                var rewrittenType = MergeOutputType(source.InnerType(), nullableTarget.InnerType());

                if (rewrittenType is not null)
                {
                    return new NonNullTypeDefinition(new ListTypeDefinition(rewrittenType));
                }
            }

            return null;
        }

        if (source.Kind == target.Kind && target.IsListType() && source.IsListType())
        {
            if (source.Kind is TypeKind.NonNull)
            {
                var rewrittenType = MergeInputType(source.InnerType().InnerType(), target.InnerType().InnerType());

                if (rewrittenType is not null)
                {
                    return new NonNullTypeDefinition(new ListTypeDefinition(rewrittenType));
                }
            }
            else
            {
                var rewrittenType = MergeInputType(source.InnerType(), target.InnerType());

                if (rewrittenType is not null)
                {
                    return new ListTypeDefinition(rewrittenType);
                }
            }
        }

        return null;
    }

    internal static void MergeDescriptionWith(this INamedTypeDefinition target, INamedTypeDefinition source)
    {
        if (string.IsNullOrWhiteSpace(target.Description) && !string.IsNullOrWhiteSpace(source.Description))
        {
            target.Description = source.Description;
        }
    }

    internal static void MergeDirectivesWith(
        this IDirectivesProvider target,
        IDirectivesProvider source,
        CompositionContext context,
        bool shouldApplySemanticNonNull = true)
    {
        foreach (var directive in source.Directives)
        {
            if (context.FusionTypes.IsFusionDirective(directive.Name)
                // @deprecated is handled separately
                || directive.Name == BuiltIns.Deprecated.Name
                // @tag is handled separately
                || directive.Name == "tag")
            {
                continue;
            }

            if (directive.Name == BuiltIns.SemanticNonNull.Name && !shouldApplySemanticNonNull)
            {
                continue;
            }

            context.FusionGraph.DirectiveDefinitions.TryGetDirective(directive.Name, out var directiveDefinition);

            if (!target.Directives.ContainsName(directive.Name))
            {
                target.Directives.Add(directive);
            }
            else
            {
                if (directive.Name.EqualsOrdinal("cost"))
                {
                    var currentCost = target.Directives.FirstOrDefault("cost")!;
                    if (currentCost.Arguments.TryGetValue("weight", out var value)
                        && value is StringValueNode stringValueNode
                        && double.TryParse(stringValueNode.Value, out var currentWeight)
                        && directive.Arguments.TryGetValue("weight", out value)
                        && value is StringValueNode newStringValueNode
                        && double.TryParse(newStringValueNode.Value, out var newWeight)
                        && newWeight > currentWeight)
                    {
                        target.Directives.Remove(currentCost);
                        target.Directives.Add(directive);
                    }

                    continue;
                }


                if (directiveDefinition is not null && directiveDefinition.IsRepeatable)
                {
                    target.Directives.Add(directive);
                }
            }
        }
    }

    internal static void MergeDescriptionWith(this DirectiveDefinition target, DirectiveDefinition source)
    {
        if (string.IsNullOrWhiteSpace(target.Description) && !string.IsNullOrWhiteSpace(source.Description))
        {
            target.Description = source.Description;
        }
    }

    internal static void MergeDescriptionWith(this EnumValue target, EnumValue source)
    {
        if (string.IsNullOrWhiteSpace(target.Description) && !string.IsNullOrWhiteSpace(source.Description))
        {
            target.Description = source.Description;
        }
    }

    internal static void MergeDescriptionWith(this IFieldDefinition target, IFieldDefinition source)
    {
        if (string.IsNullOrWhiteSpace(target.Description) && !string.IsNullOrWhiteSpace(source.Description))
        {
            target.Description = source.Description;
        }
    }

    internal static void MergeDeprecationWith(this IFieldDefinition target, IFieldDefinition source)
    {
        if (!target.IsDeprecated && source.IsDeprecated)
        {
            target.IsDeprecated = true;
        }

        if (target.IsDeprecated &&
            string.IsNullOrWhiteSpace(target.DeprecationReason) &&
            !string.IsNullOrWhiteSpace(source.DeprecationReason))
        {
            target.DeprecationReason = source.DeprecationReason;
        }
    }

    internal static void MergeDeprecationWith(this EnumValue target, EnumValue source)
    {
        if (!target.IsDeprecated && source.IsDeprecated)
        {
            target.IsDeprecated = true;
        }

        if (target.IsDeprecated &&
            string.IsNullOrWhiteSpace(target.DeprecationReason) &&
            !string.IsNullOrWhiteSpace(source.DeprecationReason))
        {
            target.DeprecationReason = source.DeprecationReason;
        }
    }
}
