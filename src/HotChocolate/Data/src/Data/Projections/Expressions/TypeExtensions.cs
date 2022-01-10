using System;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Projections.Expressions;

public static class TypeExtensions
{
    public static Type UnwrapRuntimeType(this IType type) =>
        type switch
        {
            ListType t => t.ElementType().ToRuntimeType(),
            IPageType t => t.ItemType.UnwrapRuntimeType(),
            IEdgeType t => t.NodeType.UnwrapRuntimeType(),
            NonNullType t => t.InnerType().UnwrapRuntimeType(),
            INamedType t => t.ToRuntimeType(),
            _ => throw ThrowHelper.ProjectionVisitor_CouldNotUnwrapType(type)
        };
}
