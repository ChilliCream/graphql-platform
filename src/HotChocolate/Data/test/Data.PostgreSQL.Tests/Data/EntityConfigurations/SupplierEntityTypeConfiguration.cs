using HotChocolate.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotChocolate.Data.Data.EntityConfigurations;

internal sealed class SupplierEntityTypeConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder
            .ToTable("Suppliers");

        builder
            .Property(s => s.Name)
            .HasMaxLength(100);

        builder
            .Property(s => s.Website)
            .HasMaxLength(256);

        builder
            .Property(s => s.ContactEmail)
            .HasMaxLength(256);
    }
}
