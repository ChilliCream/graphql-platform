using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Types;

namespace HotChocolate.Data.Spatial.Filters.Tests
{
    public abstract class FilterTestBase
    {
        public ISchema CreateSchema(Action<ISchemaBuilder> configure)
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddFiltering(x => x
                    .AddDefaults()
                    .AddSpatialOperations()
                    .BindSpatialTypes()
                    .Provider(
                        new QueryableFilterProvider(
                            p => p.AddSpatialHandlers().AddDefaultFieldHandlers())))
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"));

            configure(builder);

            return builder.Create();
        }
    }
}
