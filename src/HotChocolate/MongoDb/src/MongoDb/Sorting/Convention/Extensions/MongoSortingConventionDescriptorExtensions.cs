using System;
using HotChocolate.Data.Sorting;

namespace HotChocolate.MongoDb.Sorting.Convention.Extensions
{
    public static class MongoSortingConventionDescriptorExtensions
    {
        public static ISortConventionDescriptor AddMongoDbDefaults(
            this ISortConventionDescriptor descriptor) =>
            descriptor.AddDefaultOperations().BindDefaultTypes().UseMongoDbProvider();

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

            descriptor.BindRuntimeType<string, DefaultSortEnumType>();
            descriptor.DefaultBinding<DefaultSortEnumType>();

            return descriptor;
        }
    }
}
