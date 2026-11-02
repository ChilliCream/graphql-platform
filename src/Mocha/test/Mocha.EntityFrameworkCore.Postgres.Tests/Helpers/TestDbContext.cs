using Microsoft.EntityFrameworkCore;
using Mocha.Inbox;
using Mocha.Outbox;
using Mocha.Sagas.EfCore;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddPostgresInbox();
        modelBuilder.AddPostgresOutbox();
        modelBuilder.AddPostgresSagas();
        modelBuilder.AddPostgresScheduledMessages();
    }
}
