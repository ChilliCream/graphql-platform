using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Sagas.EfCore;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class SagaStateEntityConfigurationTests
{
    [Fact]
    public void Configure_Should_SetTableName_When_Applied()
    {
        var entityType = GetSagaStateEntityType();

        Assert.Equal("saga_states", entityType.GetTableName());
    }

    [Fact]
    public void Configure_Should_SetCompositeKey_When_Applied()
    {
        var entityType = GetSagaStateEntityType();
        var pk = entityType.FindPrimaryKey()!;
        var pkPropertyNames = pk.Properties.Select(p => p.Name).ToArray();

        Assert.Equal(2, pkPropertyNames.Length);
        Assert.Equal(nameof(SagaState.Id), pkPropertyNames[0]);
        Assert.Equal(nameof(SagaState.SagaName), pkPropertyNames[1]);
    }

    [Fact]
    public void Configure_Should_SetVersionAsConcurrencyToken_When_Applied()
    {
        var entityType = GetSagaStateEntityType();
        var versionProp = entityType.FindProperty(nameof(SagaState.Version))!;

        Assert.True(versionProp.IsConcurrencyToken);
    }

    [Fact]
    public void Configure_Should_SetStateColumnType_When_Applied()
    {
        var entityType = GetSagaStateEntityType();
        var stateProp = entityType.FindProperty(nameof(SagaState.State))!;

        Assert.Equal("json", stateProp.GetColumnType());
    }

    [Fact]
    public void Configure_Should_CreateCreatedAtIndex_When_Applied()
    {
        var entityType = GetSagaStateEntityType();
        var createdAtProp = entityType.FindProperty(nameof(SagaState.CreatedAt))!;
        var index = entityType.FindIndex(createdAtProp)!;

        Assert.Equal("ix_saga_states_created_at", index.GetDatabaseName());
    }

    [Fact]
    public void Configure_Should_SetColumnNames_When_Applied()
    {
        var entityType = GetSagaStateEntityType();

        Assert.Equal("id", entityType.FindProperty(nameof(SagaState.Id))!.GetColumnName());
        Assert.Equal("saga_name", entityType.FindProperty(nameof(SagaState.SagaName))!.GetColumnName());
        Assert.Equal("state", entityType.FindProperty(nameof(SagaState.State))!.GetColumnName());
        Assert.Equal("version", entityType.FindProperty(nameof(SagaState.Version))!.GetColumnName());
        Assert.Equal("created_at", entityType.FindProperty(nameof(SagaState.CreatedAt))!.GetColumnName());
        Assert.Equal("updated_at", entityType.FindProperty(nameof(SagaState.UpdatedAt))!.GetColumnName());
    }

    private static IEntityType GetSagaStateEntityType()
    {
        var model = CreateModel();
        return model.FindEntityType(typeof(SagaState))!;
    }

    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseNpgsql("Host=localhost").Options;
        using var context = new TestDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }
}
