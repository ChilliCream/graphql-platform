using HotChocolate.Data.Filters;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Tests
{
    public class ExtensionTests
    {
        [Fact]
        public void Convention_DefaultScope_Extensions()
        {
            // arrange
            // act
            var convention = new FilterConvention(
                x => x.UseDefault()
                    .Extension<StringOperationInput>(
                        y => y.Operation(Operations.Like).Type<StringType>())
                    .Operation(Operations.Like).Name("like"));

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(convention)
                .AddTypeInterceptor<FilterTypeInterceptor>()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar")
                        .Argument("test", x => x.Type<TestFilter>()));

            ISchema? schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class TestFilter : FilterInputType
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                descriptor.Field("test").Type<StringOperationInput>();
            }
        }
    }
}