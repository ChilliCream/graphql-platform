using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal static class TypeMergeExtensions
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
                return new NonNullType(source);
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
}