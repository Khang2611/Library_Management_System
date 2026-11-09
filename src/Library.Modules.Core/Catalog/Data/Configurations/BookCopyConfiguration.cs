using Library.Modules.Core.Catalog.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Catalog.Data.Configurations;

public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies", t =>
        {
            t.HasCheckConstraint("CK_BookCopies_Condition", "[Condition] IN (N'Good', N'MinorDamage', N'MajorDamage', N'Lost')");
            t.HasCheckConstraint("CK_BookCopies_Status", "[Status] IN (N'Available', N'Reserved', N'Borrowed', N'Maintenance', N'Lost', N'Inactive')");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Barcode)
            .IsUnicode(false)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Condition)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(CopyCondition.Good);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(CopyStatus.Available);

        builder.Property(c => c.AcquiredDate)
            .HasDefaultValueSql("CAST(GETDATE() AS DATE)");

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(c => c.RowVersion)
            .IsRowVersion();

        // Indexes
        builder.HasIndex(c => c.Barcode)
            .IsUnique()
            .HasDatabaseName("UQ_BookCopies_Barcode");

        builder.HasIndex(c => c.BookId)
            .HasDatabaseName("IX_BookCopies_BookId");

        builder.HasIndex(c => c.LocationId)
            .HasDatabaseName("IX_BookCopies_LocationId");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_BookCopies_Status");

        // Relationships
        builder.HasOne(c => c.Book)
            .WithMany(b => b.Copies)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Location)
            .WithMany(l => l.Copies)
            .HasForeignKey(c => c.LocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
