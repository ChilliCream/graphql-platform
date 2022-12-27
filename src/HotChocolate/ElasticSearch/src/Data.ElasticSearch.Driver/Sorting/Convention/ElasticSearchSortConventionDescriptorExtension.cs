using HotChocolate.Data.ElasticSearch.Sorting.Handlers;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.ElasticSearch.Sorting.Convention;

public static class ElasticSearchSortConventionDescriptorExtension
{
    public static ISortConventionDescriptor AddElasticSearchDefaults(
        this ISortConventionDescriptor descriptor) =>
        descriptor
            .AddDefaultElasticSearchOperations()
            .BindDefaultElasticSearchTypes()
            .UseElasticSearchProvider();

    public static ISortConventionDescriptor AddDefaultElasticSearchOperations(this ISortConventionDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Operation(DefaultSortOperations.Ascending).Name("ASC");
        descriptor.Operation(DefaultSortOperations.Descending).Name("DESC");

        return descriptor;
    }

    public static ISortConventionDescriptor BindDefaultElasticSearchTypes(
        this ISortConventionDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.BindRuntimeType<string, DefaultSortEnumType>();
        descriptor.DefaultBinding<DefaultSortEnumType>();

        return descriptor;
    }

    public static ISortConventionDescriptor UseElasticSearchProvider(this ISortConventionDescriptor descriptor)
        => descriptor.Provider(new ElasticSearchSortProvider(x => x
            .AddDefaultFieldHandlers()));

    public static ISortProviderDescriptor<ElasticSearchSortVisitorContext> AddDefaultFieldHandlers(
        this ISortProviderDescriptor<ElasticSearchSortVisitorContext> descriptor)
    {
        descriptor.AddOperationHandler<ElasticSearchAscendingSortHandler>();
        descriptor.AddOperationHandler<ElasticSearchDescendingSortHandler>();
        descriptor.AddFieldHandler<ElasticSearchDefaultSortFieldHandler>();
        return descriptor;
    }
}
