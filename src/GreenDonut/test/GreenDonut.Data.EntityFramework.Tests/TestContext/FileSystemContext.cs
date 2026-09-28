using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data.TestContext;

public class FileSystemContext(string connectionString, IEnumerable<IInterceptor>? interceptors = null) : DbContext
{
    public DbSet<FileSystemEntry> Entries => Set<FileSystemEntry>();

    public DbSet<FileSystemDirectory> Directories => Set<FileSystemDirectory>();

    public DbSet<FileSystemFile> Files => Set<FileSystemFile>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString);

        if (interceptors is not null)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<FileSystemEntry>()
            .UseTpcMappingStrategy()
            .HasKey(e => e.Id);

        modelBuilder
            .Entity<FileSystemDirectory>()
            .HasMany(d => d.Children)
            .WithOne(e => e.Parent)
            .HasForeignKey(e => e.ParentId)
            .IsRequired(false);

        base.OnModelCreating(modelBuilder);
    }
}
