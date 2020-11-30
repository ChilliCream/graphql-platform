using System;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data
{
    public static class SortConventionDescriptorExtensions
    {
        public static ISortConventionDescriptor AddDefaults(
            this ISortConventionDescriptor descriptor) =>
            descriptor.AddDefaultOperations().BindDefaultTypes().UseQueryableProvider();

        public static ISortConventionDescriptor AddDefaultOperations(
            this ISortConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Operation(DefaultSortOperations.Ascending).Name("ASC");
            descriptor.Operation(DefaultSortOperations.Descending).Name("DESC");
            return descriptor;
        }

        public static ISortConventionDescriptor BindDefaultTypes(
            this ISortConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            // bind string and structs as it is a class to avoid SortFilterInputType<string>,..
            descriptor
                .BindRuntimeType<Guid?, DefaultSortEnumType>()
                .BindRuntimeType<DateTime?, DefaultSortEnumType>()
                .BindRuntimeType<DateTimeOffset?, DefaultSortEnumType>()
                .BindRuntimeType<TimeSpan?, DefaultSortEnumType>()
                .BindRuntimeType<Guid, DefaultSortEnumType>()
                .BindRuntimeType<DateTime, DefaultSortEnumType>()
                .BindRuntimeType<DateTimeOffset, DefaultSortEnumType>()
                .BindRuntimeType<TimeSpan?, DefaultSortEnumType>()
                .BindRuntimeType<string, DefaultSortEnumType>();

            descriptor.DefaultBinding<DefaultSortEnumType>();

            return descriptor;
        }
    }
}
