namespace HotChocolate.Data.Filters.Demo
{
    public class MongoFilterConvention : FilterConvention
    {
        protected override void Configure(
              IFilterConventionDescriptor descriptor)
        {
            descriptor
                .OfType<Comparable>()
                .OfType<DateTime>()
                .Types(x => x.Field<StringFieldConvention>().UseSnakeCase())
                .Visitor<FilterDefinition<BsonDocument>>(x => x.UseSpatial());
        }
    }

    public class SqlFilterConvention : FilterConvention<Expression>
    {
        protected override void Configure(
            IFilterConventionDescriptor descriptor)
        {
            descriptor
                .Types(x => x.Field<StringFieldConvention>())
                .Visitor(x => x.UseSpatial());
        }
    }
}
