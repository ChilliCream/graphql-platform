using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
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
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(
                    new FilterConvention(
                        x => x.UseDefault()
                            .Extension<StringOperationInput>(
                                y => y.Operation(Operations.Like))
                            .Operation(Operations.Like).Name("like")))
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"));

            ISchema? schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}