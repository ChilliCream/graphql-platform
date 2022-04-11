
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Data.Tests;

public class FilterOperationFieldExtensionTests
{
    [Fact]
    public async Task AllowsShouldAddOperationsToFilterField()
    {
        // arrange
        FilterFieldDefinition? definition = null;

        // act
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>(x => x
                .Field(x => x.GetBars())
                .UseFiltering<Bar>(
                    x =>
                    {
                        var descriptor = x.Field(x => x.Name);
                        definition = descriptor.Extend().Definition;
                        descriptor
                            .AllowAnd()
                            .AllowOr()
                            .AllowContains()
                            .AllowEndsWith()
                            .AllowEquals()
                            .AllowGreaterThan()
                            .AllowGreaterThanOrEquals()
                            .AllowIn()
                            .AllowStartsWith()
                            .AllowLowerThan()
                            .AllowLowerThanOrEquals()
                            .AllowNotContains()
                            .AllowNotEndsWith()
                            .AllowNotEquals()
                            .AllowNotGreaterThan()
                            .AllowNotGreaterThanOrEquals()
                            .AllowNotIn()
                            .AllowNotLowerThan()
                            .AllowNotStartsWith()
                            .AllowNotLowerThanOrEquals();
                    }))
            .AddFiltering()
            .BuildSchemaAsync();

        // assert
        Assert.NotNull(definition);
        Assert.Contains(DefaultFilterOperations.Equals, definition!.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotEquals, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.Contains, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotContains, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.In, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotIn, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.StartsWith, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotStartsWith, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.EndsWith, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotEndsWith, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.And, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.Or, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.GreaterThan, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotGreaterThan, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.GreaterThanOrEquals, definition.AllowedOperations);
        Assert.Contains(
            DefaultFilterOperations.NotGreaterThanOrEquals,
            definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.LowerThan, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotLowerThan, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.LowerThanOrEquals, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotLowerThanOrEquals, definition.AllowedOperations);
    }

    [Fact]
    public async Task AllowsShouldAddOperationsToFilterOperationField()
    {
        // arrange
        FilterFieldDefinition? definition = null;

        // act
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>(x => x
                .Field(x => x.GetBars())
                .UseFiltering<Bar>(
                    x =>
                    {
                        var descriptor = x
                            .Operation(DefaultFilterOperations.Data)
                            .Type<StringOperationFilterInputType>();
                        definition = descriptor.Extend().Definition;
                        descriptor
                            .AllowAnd()
                            .AllowOr()
                            .AllowContains()
                            .AllowEndsWith()
                            .AllowEquals()
                            .AllowGreaterThan()
                            .AllowGreaterThanOrEquals()
                            .AllowIn()
                            .AllowStartsWith()
                            .AllowLowerThan()
                            .AllowLowerThanOrEquals()
                            .AllowNotContains()
                            .AllowNotEndsWith()
                            .AllowNotEquals()
                            .AllowNotGreaterThan()
                            .AllowNotGreaterThanOrEquals()
                            .AllowNotIn()
                            .AllowNotLowerThan()
                            .AllowNotStartsWith()
                            .AllowNotLowerThanOrEquals();
                    }))
            .AddFiltering()
            .BuildSchemaAsync();

        // assert
        Assert.NotNull(definition);
        Assert.Contains(DefaultFilterOperations.Equals, definition!.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotEquals, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.Contains, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotContains, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.In, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotIn, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.StartsWith, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotStartsWith, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.EndsWith, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotEndsWith, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.And, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.Or, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.GreaterThan, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotGreaterThan, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.GreaterThanOrEquals, definition.AllowedOperations);
        Assert.Contains(
            DefaultFilterOperations.NotGreaterThanOrEquals,
            definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.LowerThan, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotLowerThan, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.LowerThanOrEquals, definition.AllowedOperations);
        Assert.Contains(DefaultFilterOperations.NotLowerThanOrEquals, definition.AllowedOperations);
    }

    public class Query
    {
        public IReadOnlyList<Bar> GetBars() => Array.Empty<Bar>();
    }

    public class Bar
    {
        public string Name { get; set; }
    }
}
