using Library.Modules.Core.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Circulation.Data.Configurations;

public class LoanSlipConfiguration : IEntityTypeConfiguration<LoanSlip>
{
    public void Configure(EntityTypeBuilder<LoanSlip> builder)
    {
        builder.ToTable("LoanSlips");
        builder.HasKey(ls => ls.Id);

        builder.Property(ls => ls.BorrowedAt).HasDefaultValueSql("SYSDATETIME()");
        builder.Property(ls => ls.Notes).HasMaxLength(500);

        builder.Property(ls => ls.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(LoanStatus.Borrowing);

        // CK_PhieuMuon_NgayHenTra
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_LoanSlips_DueDate",
            "[DueDate] >= CAST([BorrowedAt] AS DATE)"));

        // Cross-context FKs (no nav properties)
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(ls => ls.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(ls => ls.LibrarianId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Within Circulation context navigation
        builder.HasMany(ls => ls.Details)
            .WithOne(d => d.LoanSlip)
            .HasForeignKey(d => d.LoanSlipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
