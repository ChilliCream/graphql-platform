using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Inbox;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class InboxEntityConfigurationTests
{
    [Fact]
    public void Configure_Should_SetTableName_When_Applied()
    {
        var entityType = GetInboxMessageEntityType();

        Assert.Equal("inbox_messages", entityType.GetTableName());
    }

    [Fact]
    public void Configure_Should_SetCompositePrimaryKey_When_Applied()
    {
        var entityType = GetInboxMessageEntityType();
        var pk = entityType.FindPrimaryKey()!;

        Assert.Equal(2, pk.Properties.Count);
        Assert.Equal(nameof(InboxMessage.MessageId), pk.Properties[0].Name);
        Assert.Equal(nameof(InboxMessage.ConsumerType), pk.Properties[1].Name);
    }

    [Fact]
    public void Configure_Should_CreateProcessedAtIndex_When_Applied()
    {
        var entityType = GetInboxMessageEntityType();
        var processedAtProp = entityType.FindProperty(nameof(InboxMessage.ProcessedAt))!;
        var index = entityType.FindIndex(processedAtProp)!;

        Assert.Equal("ix_inbox_messages_processed_at", index.GetDatabaseName());
    }

    [Fact]
    public void Configure_Should_SetColumnNames_When_Applied()
    {
        var entityType = GetInboxMessageEntityType();

        Assert.Equal("message_id", entityType.FindProperty(nameof(InboxMessage.MessageId))!.GetColumnName());
        Assert.Equal("consumer_type", entityType.FindProperty(nameof(InboxMessage.ConsumerType))!.GetColumnName());
        Assert.Equal("message_type", entityType.FindProperty(nameof(InboxMessage.MessageType))!.GetColumnName());
        Assert.Equal("processed_at", entityType.FindProperty(nameof(InboxMessage.ProcessedAt))!.GetColumnName());
    }

    [Fact]
    public void Configure_Should_SetMaxLength_When_Applied()
    {
        var entityType = GetInboxMessageEntityType();

        Assert.Equal(512, entityType.FindProperty(nameof(InboxMessage.MessageId))!.GetMaxLength());
        Assert.Equal(512, entityType.FindProperty(nameof(InboxMessage.ConsumerType))!.GetMaxLength());
        Assert.Equal(512, entityType.FindProperty(nameof(InboxMessage.MessageType))!.GetMaxLength());
    }

    private static IEntityType GetInboxMessageEntityType()
    {
        var model = CreateModel();
        return model.FindEntityType(typeof(InboxMessage))!;
    }

    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseNpgsql("Host=localhost").Options;
        using var context = new TestDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }
}
