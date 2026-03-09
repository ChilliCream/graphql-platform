using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotChocolate.Data.TestContext.EntityConfigurations;

internal sealed class ProductEntityTypeConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
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

        /*
        builder
            .Property(ci => ci.Embedding)
            .HasColumnType("vector(1536)");
            */

        builder
            .HasOne(ci => ci.Brand)
            .WithMany(ci => ci.Products);

        builder
            .HasOne(ci => ci.Type)
            .WithMany(ci => ci.Products);

        builder
            .HasIndex(ci => ci.Name);
    }
}
