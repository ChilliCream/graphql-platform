using Microsoft.EntityFrameworkCore;

namespace Spatial.Demo
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<County>(
                entity =>
                {
                    entity.HasKey(e => e.Xid)
                        .HasName("county_boundaries_pkey");

                    entity.ToTable("county_boundaries", "boundaries");

                    entity.HasIndex(e => e.Shape)
                        .HasName("county_boundaries_shape_geom_idx")
                        .HasMethod("gist");

                    entity.Property(e => e.Xid).HasColumnName("xid");

                    entity.Property(e => e.Color4)
                        .HasColumnName("color4")
                        .HasColumnType("numeric(5,0)");

                    entity.Property(e => e.Countynbr)
                        .HasColumnName("countynbr")
                        .HasMaxLength(2);

                    entity.Property(e => e.Entitynbr)
                        .HasColumnName("entitynbr")
                        .HasColumnType("numeric(38,8)");

                    entity.Property(e => e.Entityyr)
                        .HasColumnName("entityyr")
                        .HasColumnType("numeric(38,8)");

                    entity.Property(e => e.Fips)
                        .HasColumnName("fips")
                        .HasColumnType("numeric(38,8)");

                    entity.Property(e => e.FipsStr)
                        .HasColumnName("fips_str")
                        .HasMaxLength(5);

                    entity.Property(e => e.Name)
                        .HasColumnName("name")
                        .HasMaxLength(100);

                    entity.Property(e => e.PopCurrestimate)
                        .HasColumnName("pop_currestimate")
                        .HasColumnType("numeric(10,0)");

                    entity.Property(e => e.PopLastcensus)
                        .HasColumnName("pop_lastcensus")
                        .HasColumnType("numeric(10,0)");

                    entity.Property(e => e.Shape)
                        .HasColumnName("shape")
                        .HasColumnType("geometry(MultiPolygon,26912)");

                    entity.Property(e => e.Stateplane)
                        .HasColumnName("stateplane")
                        .HasMaxLength(20);

                    entity.Property(e => e.Shape)
                        .HasColumnName("shape")
                        .HasColumnType("geometry");
                });
        }

        public DbSet<County> Counties { get; set; } = default!;
    }
}
