using HotChocolate.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotChocolate.Data.Data.EntityConfigurations;

internal sealed class ProductTypeEntityTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder
            .ToTable("ProductTypes");

        builder
            .Property(cb => cb.Name)
            .HasMaxLength(100);
    }
}
