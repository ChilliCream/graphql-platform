using System;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters
{
    public class MockFilterConvention
        : FilterConvention
    {
        public MockFilterConvention()
        {
        }

        public MockFilterConvention(
            Action<IFilterConventionDescriptor> descriptor) : base(descriptor)
        {
        }

        protected override void Configure(IFilterConventionDescriptor descriptor)
        {
            descriptor.UseMongoVisitor().UseDefault();
        }

        public FilterConventionDefinition GetConventionDefinition()
        {
            return GetOrCreateConfiguration();
        }

        public FilterVisitorDefinition<FilterDefinition<BsonDocument>> GetExpressionDefinition()
        {
            return GetOrCreateConfiguration().VisitorDefinition
                as FilterVisitorDefinition<FilterDefinition<BsonDocument>>;
        }

        public new static MockFilterConvention Default { get; } =
            new MockFilterConvention();
    }
}
