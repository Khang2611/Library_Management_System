using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Circulation.Data.Configurations;

public class RenewalConfiguration : IEntityTypeConfiguration<Renewal>
{
    public void Configure(EntityTypeBuilder<Renewal> builder)
    {
        builder.ToTable("Renewals", t =>
        {
            t.HasCheckConstraint("CK_Renewals_RenewalNumber", "[RenewalNumber] > 0");
            t.HasCheckConstraint("CK_Renewals_NewDueDate", "[NewDueDate] > [OldDueDate]");
            t.HasCheckConstraint("CK_Renewals_Status", "[Status] IN (N'Pending', N'Approved', N'Rejected', N'Cancelled')");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestedAt).HasDefaultValueSql("SYSDATETIME()");
        builder.Property(r => r.Reason).HasMaxLength(500);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RenewalStatus.Pending);

        // UQ_Renewals_Detail_RenewalNumber: (LoanSlipDetailId, RenewalNumber)
        builder.HasIndex(r => new { r.LoanSlipDetailId, r.RenewalNumber })
            .IsUnique()
            .HasDatabaseName("UQ_Renewals_Detail_RenewalNumber");

        // Indexes
        builder.HasIndex(r => r.LibrarianId)
            .HasDatabaseName("IX_Renewals_LibrarianId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Renewals_Status");

        builder.HasOne(r => r.LoanSlipDetail)
            .WithMany(d => d.Renewals)
            .HasForeignKey(r => r.LoanSlipDetailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
