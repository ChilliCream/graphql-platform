using HotChocolate.Data.Sorting;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Tests
{
    public class ExtensionTests
    {
        [Fact]
        public void Convention_DefaultScope_Extensions_Enum()
        {
            // arrange
            // act
            var convention = new SortConvention(
                x => x.UseMock()
                    .ConfigureEnum<DefaultSortEnumType>(y => y.Operation(123))
                    .Operation(123).Name("test"));

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<ISortConvention>(convention)
                .AddTypeInterceptor<SortTypeInterceptor>()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar")
                        .Argument("test", x => x.Type<TestSort>()));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_DefaultScope_Extensions()
        {
            // arrange
            // act
            var convention = new SortConvention(
                x => x.UseMock()
                    .Configure<TestSort>(
                        y => y.Field("foo").Type<DefaultSortEnumType>())
                    .Operation(123).Name("test"));

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<ISortConvention>(convention)
                .AddTypeInterceptor<SortTypeInterceptor>()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar")
                        .Argument("test", x => x.Type<TestSort>()));

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class TestSort : SortInputType
        {
            protected override void Configure(ISortInputTypeDescriptor descriptor)
            {
                descriptor.Field("test").Type<DefaultSortEnumType>();
            }
        }
    }
}
