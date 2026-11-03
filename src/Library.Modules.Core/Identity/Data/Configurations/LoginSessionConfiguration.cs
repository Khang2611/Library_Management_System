using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Identity.Data.Configurations;

public class LoginSessionConfiguration : IEntityTypeConfiguration<LoginSession>
{
    public void Configure(EntityTypeBuilder<LoginSession> builder)
    {
        builder.ToTable("LoginSessions", t =>
        {
            t.HasCheckConstraint("CK_LoginSessions_ExpiresAt", "[ExpiresAt] > [LoggedInAt]");
        });

        builder.HasKey(ls => ls.Id);

        builder.Property(ls => ls.RefreshTokenHash)
            .HasMaxLength(255);

        builder.Property(ls => ls.IpAddress)
            .IsUnicode(false)
            .HasMaxLength(45); // IPv6 max

        builder.Property(ls => ls.UserAgent)
            .HasMaxLength(500);

        builder.Property(ls => ls.LoggedInAt)
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasIndex(ls => ls.AccountId)
            .HasDatabaseName("IX_LoginSessions_AccountId");

        builder.HasIndex(ls => ls.ExpiresAt)
            .HasDatabaseName("IX_LoginSessions_ExpiresAt");

        builder.HasOne(ls => ls.Account)
            .WithMany(a => a.LoginSessions)
            .HasForeignKey(ls => ls.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
