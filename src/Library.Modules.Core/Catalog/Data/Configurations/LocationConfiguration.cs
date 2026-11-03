using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Catalog.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations", t =>
        {
            t.HasCheckConstraint("CK_Locations_RowLevel", "[RowLevel] > 0");
            t.HasCheckConstraint("CK_Locations_MaxCapacity", "[MaxCapacity] IS NULL OR [MaxCapacity] > 0");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Aisle)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Shelf)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.RowLevel)
            .IsRequired();

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasIndex(l => new { l.Aisle, l.Shelf, l.RowLevel })
            .IsUnique()
            .HasDatabaseName("UQ_Locations_Coordinates");

        builder.HasMany(l => l.Copies)
            .WithOne(c => c.Location)
            .HasForeignKey(c => c.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

