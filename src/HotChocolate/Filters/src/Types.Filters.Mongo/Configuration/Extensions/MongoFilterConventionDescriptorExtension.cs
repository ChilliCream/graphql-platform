using HotChocolate.Types.Filters.Conventions;
<<<<<<< Updated upstream
=======
using MongoDB.Bson;
>>>>>>> Stashed changes
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class MongoFilterConventionDescriptorExtension
    {
<<<<<<< Updated upstream
        public static IFilterVisitorDescriptor<IMongoQuery> UseExpressionVisitor(
            this IFilterConventionDescriptor descriptor)
        {
            var desc = FilterVisitorDescriptor<IMongoQuery>.New(descriptor);
=======
        public static IFilterVisitorDescriptor<FilterDefinition<BsonDocument>>
            UseExpressionVisitor(this IFilterConventionDescriptor descriptor)
        {

            var desc = FilterVisitorDescriptor<FilterDefinition<BsonDocument>>.New(descriptor);
>>>>>>> Stashed changes
            descriptor.Visitor(desc);
            return desc;
        }
    }
}
