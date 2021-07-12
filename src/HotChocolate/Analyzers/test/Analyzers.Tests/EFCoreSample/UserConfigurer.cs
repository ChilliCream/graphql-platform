using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotChocolate.Analyzers.EFCoreSample
{
    public class UserConfigurer : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder
                .HasKey(p => p.UserId);

            builder
                .ToTable("ChilliCreamUsers");

            builder
                .Property(b => b.Username)
                .HasColumnName("user_name");

            builder
                .HasIndex(b => b.Email)
                .IsUnique();

            builder
                .Property(b => b.Email)
                .HasMaxLength(500);

            builder
                .HasKey(p => p.UserId);
        }
    }
}
