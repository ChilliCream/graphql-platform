using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class OutboxEntityConfigurationTests
{
    [Fact]
    public void Configure_Should_SetTableName_When_Applied()
    {
        var entityType = GetOutboxMessageEntityType();

        Assert.Equal("outbox_messages", entityType.GetTableName());
    }

    [Fact]
    public void Configure_Should_SetPrimaryKey_When_Applied()
    {
        var entityType = GetOutboxMessageEntityType();
        var pk = entityType.FindPrimaryKey()!;

        var pkProperty = Assert.Single(pk.Properties);
        Assert.Equal(nameof(OutboxMessage.Id), pkProperty.Name);
    }

    [Fact]
    public void Configure_Should_SetEnvelopeColumnType_When_Applied()
    {
        var entityType = GetOutboxMessageEntityType();
        var envelopeProp = entityType.FindProperty(nameof(OutboxMessage.Envelope))!;

        Assert.Equal("json", envelopeProp.GetColumnType());
    }

    [Fact]
    public void Configure_Should_CreateCreatedAtIndex_When_Applied()
    {
        var entityType = GetOutboxMessageEntityType();
        var createdAtProp = entityType.FindProperty(nameof(OutboxMessage.CreatedAt))!;
        var index = entityType.FindIndex(createdAtProp)!;

        Assert.Equal("ix_outbox_messages_created_at", index.GetDatabaseName());

        Assert.NotNull(index.IsDescending);
        Assert.Empty(index.IsDescending);
    }

    [Fact]
    public void Configure_Should_CreateTimesSentIndex_When_Applied()
    {
        var entityType = GetOutboxMessageEntityType();
        var timesSentProp = entityType.FindProperty(nameof(OutboxMessage.TimesSent))!;
        var index = entityType.FindIndex(timesSentProp)!;

        Assert.Equal("ix_outbox_messages_times_sent", index.GetDatabaseName());
    }

    [Fact]
    public void Configure_Should_SetColumnNames_When_Applied()
    {
        var entityType = GetOutboxMessageEntityType();

        Assert.Equal("id", entityType.FindProperty(nameof(OutboxMessage.Id))!.GetColumnName());
        Assert.Equal("envelope", entityType.FindProperty(nameof(OutboxMessage.Envelope))!.GetColumnName());
        Assert.Equal("times_sent", entityType.FindProperty(nameof(OutboxMessage.TimesSent))!.GetColumnName());
        Assert.Equal("created_at", entityType.FindProperty(nameof(OutboxMessage.CreatedAt))!.GetColumnName());
    }

    private static IEntityType GetOutboxMessageEntityType()
    {
        var model = CreateModel();
        return model.FindEntityType(typeof(OutboxMessage))!;
    }

    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseNpgsql("Host=localhost").Options;
        using var context = new TestDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }
}
