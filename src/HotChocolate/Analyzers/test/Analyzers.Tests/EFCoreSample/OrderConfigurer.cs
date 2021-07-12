using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotChocolate.Analyzers.EFCoreSample
{
    public class OrderConfigurer : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder
                .HasKey(p => p.OrderId);

            builder
                .HasMany(p => p.Products)
                .WithMany(b => b.Orders)
                .UsingEntity(j => j.ToTable("ProductOrders"));
        }
    }

    //#region Order



    //#endregion

    //#region Product

    //builder
    //    .HasKey(p => p.ProductId);

    //#endregion

    //#region User

    //builder
    //    .HasKey(p => p.UserId);

    //builder
    //    .ToTable("ChilliCreamUsers");

    //builder
    //    .Property(b => b.Username)
    //    .HasColumnName("user_name");

    //builder
    //    .HasIndex(b => b.Email)
    //    .IsUnique();

    //builder
    //    .Property(b => b.Email)
    //    .HasMaxLength(500);

    //builder
    //    .HasKey(p => p.UserId);

    //#endregion
}
