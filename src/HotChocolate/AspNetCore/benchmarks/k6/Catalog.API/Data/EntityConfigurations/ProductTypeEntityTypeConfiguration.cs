using eShop.Catalog.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.EntityConfigurations;

internal sealed class ProductTypeEntityTypeConfiguration : IEntityTypeConfiguration<ProductTypeEntity>
{
    public void Configure(EntityTypeBuilder<ProductTypeEntity> builder)
    {
        builder
            .ToTable("ProductTypes");

        builder
            .Property(cb => cb.Name)
            .HasMaxLength(100);
    }
}
