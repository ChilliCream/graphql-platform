using Demo.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Mocha.Outbox;

namespace Demo.Billing.Data;

public class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<RevenueSummary> RevenueSummaries => Set<RevenueSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddPostgresOutbox();

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CustomerId).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Method).HasMaxLength(50).IsRequired();
            entity.HasOne(e => e.Invoice).WithMany(i => i.Payments).HasForeignKey(e => e.InvoiceId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalAmount).HasPrecision(18, 2);
            entity.Property(e => e.RefundedAmount).HasPrecision(18, 2);
            entity.Property(e => e.RefundPercentage).HasPrecision(5, 2);
            entity.Property(e => e.CustomerId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.HasOne(e => e.Invoice).WithMany().HasForeignKey(e => e.InvoiceId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<RevenueSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
            entity.Property(e => e.AverageOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.CompletionMode).HasMaxLength(50).IsRequired();
        });
    }
}

public class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();

        // This connection string is only used by EF tools for migrations
        // It doesn't need to be a real database - just valid enough for scaffolding
        optionsBuilder.UseNpgsql("Host=localhost;Database=billing-db;Username=postgres;Password=postgres");

        return new BillingDbContext(optionsBuilder.Options);
    }
}
