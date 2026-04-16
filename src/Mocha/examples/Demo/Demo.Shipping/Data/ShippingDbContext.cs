using Demo.Shipping.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Mocha.Inbox;
using Mocha.Outbox;

namespace Demo.Shipping.Data;

public class ShippingDbContext(DbContextOptions<ShippingDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<ReturnShipment> ReturnShipments => Set<ReturnShipment>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddPostgresOutbox();
        modelBuilder.AddPostgresInbox();

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Address).HasMaxLength(500).IsRequired();
            entity.Property(e => e.TrackingNumber).HasMaxLength(100);
            entity.Property(e => e.Carrier).HasMaxLength(100);
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TrackingNumber);
        });

        modelBuilder.Entity<ShipmentItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Shipment).WithMany(s => s.Items).HasForeignKey(e => e.ShipmentId);
        });

        modelBuilder.Entity<ReturnShipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerAddress).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CustomerId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TrackingNumber).HasMaxLength(100);
            entity.Property(e => e.LabelUrl).HasMaxLength(500);
            entity.HasOne(e => e.OriginalShipment).WithMany().HasForeignKey(e => e.OriginalShipmentId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TrackingNumber);
        });
    }
}

public class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShippingDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=shipping-db;Username=postgres;Password=postgres");
        return new ShippingDbContext(optionsBuilder.Options);
    }
}
