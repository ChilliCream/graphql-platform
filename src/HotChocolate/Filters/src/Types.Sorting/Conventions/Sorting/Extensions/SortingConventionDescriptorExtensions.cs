using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting.Conventions
{
    public static class SortingConventionDescriptorExtensions
    {
        public static ISortingConventionDescriptor UseDefault(
            this ISortingConventionDescriptor descriptor)
                => descriptor.AscendingName("ASC")
                    .DescendingName("DESC")
                    .TypeName(
                        (IDescriptorContext context, Type entityType) =>
                            context.Naming.GetTypeName(entityType, TypeKind.Object) + "Sort")
                    .OperationKindTypeName(
                        (IDescriptorContext context, Type entityType) =>
                            context.Naming.GetTypeName(entityType, TypeKind.Enum))
                    .Description(
                        (IDescriptorContext context, Type entityType) =>
                            context.Naming.GetTypeDescription(entityType, TypeKind.Object))
                    .UseImplicitSorting()
                    .UseSnakeCase()
                    .UseExpressionVisitor()
                        .UseDefault()
                    .And();

        public static ISortingConventionDescriptor UseSnakeCase(
            this ISortingConventionDescriptor descriptor)
                => descriptor.ArgumentName("order_by");

        public static ISortingConventionDescriptor UsePascalCase(
            this ISortingConventionDescriptor descriptor)
                => descriptor.ArgumentName("OrderBy");

    }
}
