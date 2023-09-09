using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal static class MergeExtensions
{
    internal static IType? MergeOutputType(IType source, IType target)
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
                    return new ListType(rewrittenType);
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
                    return new ListType(rewrittenType);
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
                    return new NonNullType(new ListType(rewrittenType));
                }
            }
            else
            {
                var rewrittenType = MergeOutputType(source.InnerType(), target.InnerType());

                if (rewrittenType is not null)
                {
                    return new ListType(rewrittenType);
                }
            }
        }

        return null;
    }

    internal static IType? MergeInputType(IType source, IType target)
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
                return new NonNullType(nullableSource);
            }

            if (nullableSource.Kind == target.Kind && nullableSource.Kind == TypeKind.List)
            {
                var rewrittenType = MergeInputType(nullableSource.InnerType(), target.InnerType());

                if (rewrittenType is not null)
                {
                    return new NonNullType(new ListType(rewrittenType));
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
                    return new NonNullType(new ListType(rewrittenType));
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
                    return new NonNullType(new ListType(rewrittenType));
                }
            }
            else
            {
                var rewrittenType = MergeInputType(source.InnerType(), target.InnerType());

                if (rewrittenType is not null)
                {
                    return new ListType(rewrittenType);
                }
            }
        }

        return null;
    }

    internal static void MergeDescriptionWith<T>(this T target, T source) where T : IHasDescription
    {
        if (string.IsNullOrWhiteSpace(target.Description) && !string.IsNullOrWhiteSpace(source.Description))
        {
            target.Description = source.Description;
        }
    } 
    
    internal static void MergeDeprecationWith<T>(this T target, T source) where T : ICanBeDeprecated
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
