using Library.Modules.Core.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Modules.Core.Search.Data.Configurations;

public class SearchHistoryConfiguration : IEntityTypeConfiguration<SearchHistory>
{
    public void Configure(EntityTypeBuilder<SearchHistory> builder)
    {
        builder.ToTable("SearchHistories", t =>
        {
            t.HasCheckConstraint(
                "CK_SearchHistories_ResultCount",
                "[ResultCount] IS NULL OR [ResultCount] >= 0");
        });

        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.Keyword).HasMaxLength(500).IsRequired();
        builder.Property(sh => sh.SearchType).HasMaxLength(30).HasDefaultValue("BOOK");
        builder.Property(sh => sh.SearchedAt).HasDefaultValueSql("SYSDATETIME()");

        builder.HasIndex(sh => sh.AccountId)
            .HasDatabaseName("IX_SearchHistories_AccountId");

        // Cross-context FK → Identity.Account (SQL: ON DELETE NO ACTION)
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(sh => sh.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
