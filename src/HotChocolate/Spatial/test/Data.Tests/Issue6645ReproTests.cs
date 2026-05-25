using HotChocolate.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial;

public class Issue6645ReproTests
{
    [Fact]
    public async Task Schema_With_Geometry_Field_Should_Build()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("regions")
                    .Resolve(Array.Empty<Region>().AsQueryable())
                    .Type<ListType<ObjectType<Region>>>()
                    .UseProjection()
                    .UseFiltering())
            .AddSpatialTypes()
            .AddFiltering()
            .AddProjections()
            .AddSpatialFiltering()
            .AddSpatialProjections()
            .Services
            .BuildServiceProvider();

        // act
        var exception = await Record.ExceptionAsync(
            async () => await services.GetRequestExecutorAsync());

        // assert
        Assert.Null(exception);
    }

    private sealed class Region
    {
        public Guid Id { get; set; }

        public Geometry? Geometry { get; set; }

        public string AgreementNumber { get; set; } = string.Empty;

        public DateTime AgreementDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
