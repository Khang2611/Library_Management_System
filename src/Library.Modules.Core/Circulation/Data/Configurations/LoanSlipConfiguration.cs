using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Circulation.Data.Configurations;

public class LoanSlipConfiguration : IEntityTypeConfiguration<LoanSlip>
{
    public void Configure(EntityTypeBuilder<LoanSlip> builder)
    {
        builder.ToTable("LoanSlips", t =>
        {
            t.HasCheckConstraint(
                "CK_LoanSlips_DueDate",
                "[DueDate] >= CAST([BorrowedAt] AS DATE)");

            t.HasCheckConstraint(
                "CK_LoanSlips_Status",
                "[Status] IN (N'Pending', N'Borrowing', N'Returned', N'Overdue', N'Cancelled')");
        });

        builder.HasKey(ls => ls.Id);

        builder.Property(ls => ls.BorrowedAt).HasDefaultValueSql("SYSDATETIME()");
        builder.Property(ls => ls.Notes).HasMaxLength(500);

        builder.Property(ls => ls.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(LoanStatus.Borrowing);

        builder.Property(ls => ls.RowVersion)
            .IsRowVersion();

        builder.Ignore(ls => ls.ReturnedDate);

        // Indexes
        builder.HasIndex(ls => ls.AccountId)
            .HasDatabaseName("IX_LoanSlips_AccountId");

        builder.HasIndex(ls => ls.LibrarianId)
            .HasDatabaseName("IX_LoanSlips_LibrarianId");

        builder.HasIndex(ls => ls.Status)
            .HasDatabaseName("IX_LoanSlips_Status");

        builder.HasIndex(ls => ls.DueDate)
            .HasDatabaseName("IX_LoanSlips_DueDate");

        // Within Circulation context navigation
        builder.HasMany(ls => ls.Details)
            .WithOne(d => d.LoanSlip)
            .HasForeignKey(d => d.LoanSlipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
