using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Circulation.Data.Configurations;

public class LoanSlipDetailConfiguration : IEntityTypeConfiguration<LoanSlipDetail>
{
    public void Configure(EntityTypeBuilder<LoanSlipDetail> builder)
    {
        builder.ToTable("LoanSlipDetails", t =>
        {
            t.HasCheckConstraint("CK_LoanSlipDetails_Status", "[Status] IN (N'Borrowing', N'Returned', N'Lost', N'Damaged')");
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(LoanDetailStatus.Borrowing);

        // UQ_LoanSlipDetails_LoanSlipId_BookCopyId: (LoanSlipId, BookCopyId)
        builder.HasIndex(d => new { d.LoanSlipId, d.BookCopyId })
            .IsUnique()
            .HasDatabaseName("UQ_LoanSlipDetails_LoanSlipId_BookCopyId");

        // Indexes
        builder.HasIndex(d => d.BookCopyId)
            .HasDatabaseName("IX_LoanSlipDetails_BookCopyId");

        builder.HasIndex(d => d.Status)
            .HasDatabaseName("IX_LoanSlipDetails_Status");

        // Within Circulation
        builder.HasOne(d => d.LoanSlip)
            .WithMany(ls => ls.Details)
            .HasForeignKey(d => d.LoanSlipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Renewals)
            .WithOne(r => r.LoanSlipDetail)
            .HasForeignKey(r => r.LoanSlipDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Incidents)
            .WithOne(i => i.LoanSlipDetail)
            .HasForeignKey(i => i.LoanSlipDetailId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
