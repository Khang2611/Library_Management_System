using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Identity.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", t =>
        {
            t.HasCheckConstraint("CK_Accounts_Role", "[Role] IN (N'Reader', N'Librarian', N'Admin')");
            t.HasCheckConstraint("CK_Accounts_Status", "[Status] IN (N'Active', N'Locked', N'Inactive')");
            t.HasCheckConstraint("CK_Accounts_DeactivatedAt", "[DeactivatedAt] IS NULL OR [DeactivatedAt] > [CreatedAt]");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Email)
            .IsUnicode(false)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.PhoneNumber)
            .IsUnicode(false)
            .HasMaxLength(20);

        builder.Property(a => a.Address)
            .HasMaxLength(255);

        builder.Property(a => a.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountStatus.Active)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        // Unique constraints
        builder.HasIndex(a => a.Username).IsUnique();
        builder.HasIndex(a => a.Email).IsUnique();
        builder.HasIndex(a => a.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("IX_Accounts_PhoneNumber")
            .HasFilter("[PhoneNumber] IS NOT NULL");

        // Navigation within Identity context
        builder.HasMany(a => a.LoginSessions)
            .WithOne(ls => ls.Account)
            .HasForeignKey(ls => ls.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
