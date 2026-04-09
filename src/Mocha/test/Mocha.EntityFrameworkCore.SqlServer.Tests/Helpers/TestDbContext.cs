using Microsoft.EntityFrameworkCore;
using Mocha.Inbox;
using Mocha.Outbox;
using Mocha.Sagas.EfCore;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore.SqlServer.Tests.Helpers;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddSqlServerInbox();
        modelBuilder.AddSqlServerOutbox();
        modelBuilder.AddSqlServerSagas();
        modelBuilder.AddSqlServerScheduledMessages();
    }
}
