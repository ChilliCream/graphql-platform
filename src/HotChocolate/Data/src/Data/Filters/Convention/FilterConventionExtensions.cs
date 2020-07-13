namespace HotChocolate.Data.Filters
{
    public static class FilterConventionExtensions
    {
        public static IFilterConventionDescriptor UseDefault(
            this IFilterConventionDescriptor descriptor)
            => descriptor.UseDefaultOperations();

        public static IFilterConventionDescriptor UseDefaultOperations(
            this IFilterConventionDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Name("eq");
            descriptor.Operation(Operations.NotEquals).Name("neq");
            return descriptor;
        }
    }
}
