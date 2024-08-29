using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotChocolate.Data.TestContext.EntityConfigurations;

internal sealed class BrandEntityTypeConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder
            .ToTable("Brands");

        builder
            .Property(cb => cb.Name)
            .HasMaxLength(100);

        builder.OwnsOne(x => x.BrandDetails,
            bd => bd.OwnsOne(x => x.Country, c => c.Property(x => x.Name).HasMaxLength(100)));
    }
}
