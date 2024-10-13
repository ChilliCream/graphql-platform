using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition;

internal static class MergeExtensions
{
    internal static ITypeDefinition? MergeOutputType(ITypeDefinition source, ITypeDefinition target)
    {
        if (source.Equals(target, TypeComparison.Structural))
        {
            return target;
        }

        if (target.Kind is TypeKind.SemanticNonNull && source.Kind is not TypeKind.SemanticNonNull)
        {
            var nullableTarget = target.InnerType();

            if (source.Kind is TypeKind.NonNull)
            {
                return MergeOutputType(source, new NonNullTypeDefinition(nullableTarget));
            }

            return MergeOutputType(source, nullableTarget);
        }

        if (source.Kind is TypeKind.SemanticNonNull && target.Kind is not TypeKind.SemanticNonNull)
        {
            var nullableSource = source.InnerType();

            if (target.Kind is TypeKind.NonNull)
            {
                return MergeOutputType(new NonNullTypeDefinition(nullableSource), target);
            }

            return MergeOutputType(nullableSource, target);
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
            if (source.Kind is TypeKind.SemanticNonNull)
            {
                var rewrittenType = MergeOutputType(source.InnerType().InnerType(), target.InnerType().InnerType());

                if (rewrittenType is not null)
                {
                    return new SemanticNonNullTypeDefinition(new ListTypeDefinition(rewrittenType));
                }
            }
            else if (source.Kind is TypeKind.NonNull)
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
        CompositionContext context)
    {
        foreach (var directive in source.Directives)
        {
            if (context.FusionTypes.IsFusionDirective(directive.Name)
                || BuiltIns.IsBuiltInDirective(directive.Name)
                // @tag is handled separately
                || directive.Name == "tag")
            {
                continue;
            }

            if (context.FusionGraph.DirectiveDefinitions.TryGetDirective(directive.Name, out var directiveDefinition)
                && directiveDefinition.IsSpecDirective)
            {
                continue;
            }

            if (!target.Directives.ContainsName(directive.Name))
            {
                target.Directives.Add(directive);
            }
            else
            {
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
