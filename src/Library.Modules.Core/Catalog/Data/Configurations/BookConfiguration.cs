using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Catalog.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Isbn).HasMaxLength(20);
        builder.Property(b => b.Title).HasMaxLength(255).IsRequired();
        builder.Property(b => b.Description).HasColumnType("nvarchar(max)");
        builder.Property(b => b.Publisher).HasMaxLength(200);
        builder.Property(b => b.Language).HasMaxLength(50);
        builder.Property(b => b.CoverImageUrl).HasMaxLength(500);

        builder.Property(b => b.BookValue)
            .HasPrecision(18, 2);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(BookStatus.Active);

        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasIndex(b => b.Isbn).IsUnique().HasFilter("[ISBN] IS NOT NULL");

        // CK_Sach_NamXuatBan: 1000 <= PublishedYear <= 2100
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Books_PublishedYear",
            "[PublishedYear] IS NULL OR ([PublishedYear] BETWEEN 1000 AND 2100)"));

        // CK_Sach_SoTrang
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Books_PageCount",
            "[PageCount] IS NULL OR [PageCount] > 0"));

        // CK_Sach_GiaTriSach
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Books_BookValue",
            "[BookValue] IS NULL OR [BookValue] >= 0"));

        builder.HasMany(b => b.BookAuthors)
            .WithOne(ba => ba.Book)
            .HasForeignKey(ba => ba.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.BookCategories)
            .WithOne(bc => bc.Book)
            .HasForeignKey(bc => bc.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Copies)
            .WithOne(c => c.Book)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
