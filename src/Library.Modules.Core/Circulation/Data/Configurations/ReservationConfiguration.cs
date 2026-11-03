using Library.Modules.Core.Catalog.Data;
using Library.Modules.Core.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Circulation.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations", t =>
        {
            t.HasCheckConstraint(
                "CK_Reservations_ExpiresAt",
                "[ExpiresAt] IS NULL OR [ExpiresAt] > [ReservedAt]");

            t.HasCheckConstraint(
                "CK_Reservations_Status",
                "[Status] IN (N'Waiting', N'Ready', N'Completed', N'Cancelled', N'Expired')");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReservedAt).HasDefaultValueSql("SYSDATETIME()");

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ReservationStatus.Waiting);

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        // Indexes
        builder.HasIndex(r => r.AccountId)
            .HasDatabaseName("IX_Reservations_AccountId");

        builder.HasIndex(r => r.BookId)
            .HasDatabaseName("IX_Reservations_BookId");

        builder.HasIndex(r => r.BookCopyId)
            .HasDatabaseName("IX_Reservations_BookCopyId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Reservations_Status");

        // Cross-context FKs (no navigation properties, primitive IDs only)
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BookCopy>()
            .WithMany()
            .HasForeignKey(r => r.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
