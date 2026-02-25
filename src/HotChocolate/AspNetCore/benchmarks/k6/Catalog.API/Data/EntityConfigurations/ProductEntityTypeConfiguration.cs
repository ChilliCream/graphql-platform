using eShop.Catalog.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.EntityConfigurations;

internal sealed class ProductEntityTypeConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder
            .ToTable("Products");

        builder
            .Property(ci => ci.Name)
            .HasMaxLength(50);

        builder
            .Property(ci => ci.Description)
            .HasMaxLength(2048);

        builder
            .Property(ci => ci.ImageFileName)
            .HasMaxLength(256);

        builder
            .HasOne(ci => ci.Brand)
            .WithMany(ci => ci.Products)
            .HasForeignKey(ci => ci.BrandId);

        builder
            .HasOne(ci => ci.Type)
            .WithMany(ci => ci.Products)
            .HasForeignKey(ci => ci.TypeId);

        builder
            .HasIndex(ci => ci.Name);
    }
}
