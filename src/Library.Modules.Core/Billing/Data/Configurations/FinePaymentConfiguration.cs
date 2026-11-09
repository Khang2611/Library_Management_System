using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Billing.Data.Configurations;

public class FinePaymentConfiguration : IEntityTypeConfiguration<FinePayment>
{
    public void Configure(EntityTypeBuilder<FinePayment> builder)
    {
        builder.ToTable("FinePayments", t =>
        {
            t.HasCheckConstraint("CK_FinePayments_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_FinePayments_PaymentMethod", "[PaymentMethod] IN (N'Cash', N'BankTransfer', N'Other')");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.PaidAt).HasDefaultValueSql("SYSDATETIME()");
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Indexes
        builder.HasIndex(p => p.FineId)
            .HasDatabaseName("IX_FinePayments_FineId");

        builder.HasIndex(p => p.LibrarianId)
            .HasDatabaseName("IX_FinePayments_LibrarianId");

        // SQL: FK_FinePayments_Fines FOREIGN KEY (FineId) REFERENCES Fines (Id) ON DELETE NO ACTION
        builder.HasOne(p => p.Fine)
            .WithMany(f => f.Payments)
            .HasForeignKey(p => p.FineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
