using Microsoft.EntityFrameworkCore;
using DART.API.Net.Models;

namespace DART.API.Net.Data;

public class DartDbContext : DbContext
{
    public DartDbContext(DbContextOptions<DartDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Record> Records => Set<Record>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", table =>
                table.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Admin','Staff')"));

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(u => u.Username)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(u => u.IsActive)
                .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.UpdatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<Record>(entity =>
        {
            entity.ToTable("Records", table =>
                table.HasCheckConstraint("CK_Records_RecordType", "[RecordType] IN ('Birth','Marriage','Death')"));

            entity.HasKey(r => r.Id);

            entity.Property(r => r.RecordType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(r => r.RegistryNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(r => r.CreatedBy)
                .IsRequired();

            entity.Property(r => r.CreatedAt)
                .IsRequired();

            entity.Property(r => r.UpdatedAt)
                .IsRequired();

            entity.HasIndex(r => r.RegistryNumber);
            entity.HasIndex(r => r.Name);
            entity.HasIndex(r => r.RecordType);

            entity.HasOne(r => r.CreatedByUser)
                .WithMany(u => u.RecordsCreated)
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
