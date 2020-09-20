using System;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration
{
    public class TypeDiscoveryTests
    {
        [Fact]
        public void InferDateTime()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryWithDateTime>()
                .Create()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void InferDateTimeFromModel()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create()
                .Print()
                .MatchSnapshot();
        }

        public class QueryWithDateTime
        {
            public DateTimeOffset DateTimeOffset(DateTimeOffset time) => time;
            public DateTime DateTime(DateTime time) => time;
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("items")
                    .Type<ListType<ModelType>>()
                    .Resolver(string.Empty);

                descriptor.Field("paging")
                    .UsePaging<ModelType>()
                    .Resolver(string.Empty);
            }
        }

        public class ModelType : ObjectType<Model>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Model> descriptor)
            {
                descriptor.Field(t => t.Time)
                    .Type<NonNullType<DateTimeType>>();

                descriptor.Field(t => t.Date)
                    .Type<NonNullType<DateType>>();
            }
        }

        public class Model
        {
            public string Foo { get; set; }
            public int Bar { get; set; }
            public bool Baz { get; set; }
            public DateTime Time { get; set; }
            public DateTime Date { get; set; }
        }
    }
}