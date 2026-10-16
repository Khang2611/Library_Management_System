using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Catalog.Data.Configurations;

public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Barcode).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Location).HasMaxLength(100);

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

        builder.HasIndex(c => c.Barcode).IsUnique();

        builder.HasOne(c => c.Book)
            .WithMany(b => b.Copies)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
