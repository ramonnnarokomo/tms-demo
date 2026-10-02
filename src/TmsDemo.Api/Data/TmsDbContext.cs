using Microsoft.EntityFrameworkCore;
using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Data;

public sealed class TmsDbContext(DbContextOptions<TmsDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite doesn't store time zones: everything is saved and read back as UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Carrier>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(200);
            entity.HasIndex(c => c.Name).IsUnique();

            // Reference data available from the first run (also in tests).
            entity.HasData(
                new { Id = 1, Name = "Transportes Meseta", Email = (string?)"operaciones@meseta.example", IsActive = true },
                new { Id = 2, Name = "Levante Express", Email = (string?)"trafico@levante.example", IsActive = true },
                new { Id = 3, Name = "Cantábrico Cargo", Email = (string?)null, IsActive = false });
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.TrackingNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(s => s.TrackingNumber).IsUnique();
            entity.Property(s => s.Origin).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Destination).HasMaxLength(100).IsRequired();
            entity.Property(s => s.WeightKg).HasPrecision(10, 2);
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(s => s.Status);

            entity.HasOne(s => s.Carrier)
                .WithMany()
                .HasForeignKey(s => s.CarrierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(s => s.Events)
                .WithOne()
                .HasForeignKey(e => e.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Events is read-only from outside, so EF Core writes to the private _events list.
            entity.Navigation(s => s.Events)
                .HasField("_events")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ShipmentEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Note).HasMaxLength(500);
        });
    }
}
