namespace HotChocolate.Types.Sorting.Conventions
{
    public static class SortingConventionDescriptorExtensions
    {
        public static ISortingConventionDescriptor UseDefault(
            this ISortingConventionDescriptor descriptor)
                => descriptor.AscendingName("ASC")
                    .DescendingName("DESC")
                    .TypeName(SortingConventionDefaults.TypeName)
                    .OperationKindTypeName(SortingConventionDefaults.OperationKindTypeName)
                    .Description(SortingConventionDefaults.Description)
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
