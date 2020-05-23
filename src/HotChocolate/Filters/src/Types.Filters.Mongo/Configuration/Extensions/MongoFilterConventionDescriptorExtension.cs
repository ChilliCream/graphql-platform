using HotChocolate.Types.Filters.Conventions;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class MongoFilterConventionDescriptorExtension
    {
        public static IFilterVisitorDescriptor<IMongoQuery> UseExpressionVisitor(
            this IFilterConventionDescriptor descriptor)
        {
            var desc = FilterVisitorDescriptor<IMongoQuery>.New(descriptor);
            descriptor.Visitor(desc);
            return desc;
        }
    }
}
