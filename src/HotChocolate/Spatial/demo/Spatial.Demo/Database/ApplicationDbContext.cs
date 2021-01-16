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
            modelBuilder.Entity<Parcel>(
                entity =>
                {
                    entity.HasKey(e => e.Xid)
                        .HasName("salt_lake_county_parcels_lir_pkey");

                    entity.ToTable("salt_lake_county_parcels_lir", "cadastre");

                    entity.HasIndex(e => e.Shape)
                        .HasDatabaseName("salt_lake_county_parcels_lir_shape_geom_idx")
                        .HasMethod("gist");

                    entity.Property(e => e.Xid).HasColumnName("xid");

                    entity.Property(e => e.ParcelId)
                        .HasColumnName("parcel_id")
                        .HasMaxLength(50);

                    entity.Property(e => e.BuildingSqFt)
                        .HasColumnName("bldg_sqft")
                        .HasColumnType("numeric(38,8)");

                    entity.Property(e => e.Floors)
                        .HasColumnName("floors_cnt")
                        .HasColumnType("numeric(38,8)");

                    entity.Property(e => e.RoomCount)
                        .HasColumnName("house_cnt")
                        .HasMaxLength(10);

                    entity.Property(e => e.MarketValue)
                        .HasColumnName("total_mkt_value")
                        .HasColumnType("numeric(38,8)");

                    entity.Property(e => e.YearBuilt)
                        .HasColumnName("built_yr")
                        .HasColumnType("numeric(5,0)");

                    entity.Property(e => e.Shape)
                        .HasColumnName("shape")
                        .HasColumnType("geometry(MultiPolygon,26912)");

                    entity.Property(e => e.Shape)
                        .HasColumnName("shape")
                        .HasColumnType("geometry");
                });

            modelBuilder.Entity<LiquorStore>(
                entity => {
                entity.HasKey(e => e.Xid)
                    .HasName("liquor_stores_pkey");

                entity.ToTable("liquor_stores", "society");

                entity.HasIndex(e => e.Shape)
                    .HasDatabaseName("liquor_stores_shape_geom_idx")
                    .HasMethod("gist");

                entity.Property(e => e.Xid).HasColumnName("xid");

                entity.Property(e => e.Shape)
                    .HasColumnName("shape")
                    .HasColumnType("geometry");

                entity.Property(e => e.Shape)
                    .HasColumnName("shape")
                    .HasColumnType("geometry(Point,26912)");

                    entity.Property(e => e.StoreNumber)
                    .HasColumnName("storenumber")
                    .HasColumnType("numeric(5,0)");

                entity.Property(e => e.Zip)
                    .HasColumnName("zip")
                    .HasColumnType("numeric(10,0)");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(50);

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .HasMaxLength(20);

                entity.Property(e => e.Address)
                    .HasColumnName("address")
                    .HasMaxLength(50);

                entity.Property(e => e.City)
                    .HasColumnName("city")
                    .HasMaxLength(35);

                entity.Property(e => e.County)
                    .HasColumnName("county")
                    .HasMaxLength(25);

                entity.Property(e => e.Phone)
                    .HasColumnName("phone")
                    .HasMaxLength(15);
            });
            modelBuilder.Entity<GolfCourse>(
                entity => {
                entity.HasKey(e => e.Xid)
                    .HasName("golf_courses_pkey");

                entity.ToTable("golf_courses", "recreation");

                entity.HasIndex(e => e.Shape)
                    .HasDatabaseName("golf_courses_shape_geom_idx")
                    .HasMethod("gist");

                entity.Property(e => e.Xid).HasColumnName("xid");

                entity.Property(e => e.Shape)
                    .HasColumnName("shape")
                    .HasColumnType("geometry");

                entity.Property(e => e.Shape)
                    .HasColumnName("shape")
                    .HasColumnType("geometry(MultiPolygon,26912)");

                    entity.Property(e => e.Par)
                    .HasColumnName("par")
                    .HasColumnType("numeric(10,0)");

                entity.Property(e => e.Holes)
                    .HasColumnName("holes")
                    .HasColumnType("numeric(10,0)");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(50);

                entity.Property(e => e.City)
                    .HasColumnName("city")
                    .HasMaxLength(30);

                entity.Property(e => e.County)
                    .HasColumnName("county")
                    .HasMaxLength(30);
            });
        }

        public DbSet<Parcel> Parcels { get; set; } = default!;
        public DbSet<LiquorStore> LiquorStores { get; set; } = default!;
        public DbSet<GolfCourse> GolfCourses { get; set; } = default!;
    }
}
