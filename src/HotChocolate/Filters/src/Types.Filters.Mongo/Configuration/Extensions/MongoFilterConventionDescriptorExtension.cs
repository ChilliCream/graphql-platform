using HotChocolate.Types.Filters.Conventions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class MongoFilterConventionDescriptorExtension
    {
        public static IFilterVisitorDescriptor<FilterDefinition<BsonDocument>>
            UseExpressionVisitor(this IFilterConventionDescriptor descriptor)
        {
            var desc = FilterVisitorDescriptor<FilterDefinition<BsonDocument>>.New(descriptor);
            descriptor.Visitor(desc);
            return desc;
        }
    }
}
