using eShop.Catalog.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.Data.EntityConfigurations;

internal sealed  class BrandEntityTypeConfiguration : IEntityTypeConfiguration<BrandEntity>
{
    public void Configure(EntityTypeBuilder<BrandEntity> builder)
    {
        builder
            .ToTable("Brands");

        builder
            .Property(cb => cb.Name)
            .HasMaxLength(100);
    }
}
