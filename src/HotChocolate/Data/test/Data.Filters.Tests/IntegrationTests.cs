using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task Filtering_Should_Auto_Ignore_ObjectType_Ignored_Field()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithIgnoredField>()
            .AddType<EntityWithIgnoredFieldType>()
            .AddFiltering()
            .BuildSchemaAsync();

        // assert
        Assert.NotNull(schema);
        var filterType = Assert.IsAssignableFrom<InputObjectType>(
            schema.Types["EntityWithIgnoredFieldFilterInput"]);
        Assert.Contains(filterType.Fields, field => field.Name == "id");
        Assert.Contains(filterType.Fields, field => field.Name == "name");
        Assert.DoesNotContain(filterType.Fields, field => field.Name == "internalData");
    }

    [Fact]
    public async Task Filtering_Should_Not_Ignore_Explicitly_Bound_Ignored_ObjectType_Field()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithExplicitIgnoredFieldFilter>()
            .AddType<EntityWithIgnoredFieldType>()
            .AddFiltering()
            .BuildSchemaAsync();

        // assert
        Assert.NotNull(schema);
        var filterType = Assert.IsAssignableFrom<InputObjectType>(
            schema.Types["EntityWithIgnoredFieldFilterInput"]);
        Assert.Contains(filterType.Fields, field => field.Name == "internalData");
    }
}

public class QueryWithIgnoredField
{
    [UseFiltering]
    public IQueryable<EntityWithIgnoredField> Entities() =>
        new[]
        {
            new EntityWithIgnoredField { Id = 1, Name = "A", InternalData = "A1" },
            new EntityWithIgnoredField { Id = 2, Name = "B", InternalData = "B1" }
        }.AsQueryable();
}

public class QueryWithExplicitIgnoredFieldFilter
{
    [UseFiltering(typeof(EntityWithIgnoredFieldFilterType))]
    public IQueryable<EntityWithIgnoredField> Entities() =>
        new[]
        {
            new EntityWithIgnoredField { Id = 1, Name = "A", InternalData = "A1" },
            new EntityWithIgnoredField { Id = 2, Name = "B", InternalData = "B1" }
        }.AsQueryable();
}

public class EntityWithIgnoredField
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? InternalData { get; set; }
}

public class EntityWithIgnoredFieldType : ObjectType<EntityWithIgnoredField>
{
    protected override void Configure(IObjectTypeDescriptor<EntityWithIgnoredField> descriptor)
    {
        descriptor.Ignore(t => t.InternalData);
    }
}

public class EntityWithIgnoredFieldFilterType : FilterInputType<EntityWithIgnoredField>
{
    protected override void Configure(IFilterInputTypeDescriptor<EntityWithIgnoredField> descriptor)
    {
        descriptor.Field(t => t.InternalData);
    }
}
