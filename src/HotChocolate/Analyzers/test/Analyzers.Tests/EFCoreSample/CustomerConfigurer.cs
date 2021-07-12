using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotChocolate.Analyzers.EFCoreSample
{
    public class CustomerConfigurer : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder
                .HasKey(p => p.Id)
                .HasName("PK_CustomerId");

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder
                .HasIndex(c => new { c.FirstName, c.LastName });

            builder
                .HasOne(p => p.User)
                .WithOne(b => b!.Customer!) // TODO: Is that actually correct? Seems like too many !s
                .HasForeignKey<User>(b => b.CustomerId);

            builder
                .Property(b => b.ShippingAddresses)
                .HasColumnType("jsonb");

            builder
                .HasMany(p => p.Orders)
                .WithOne(b => b.Customer)
                .HasForeignKey(p => p.CustomerId);
        }
    }
}
