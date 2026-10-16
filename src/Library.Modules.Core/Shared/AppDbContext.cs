using Library.Modules.Core.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Shared;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Module Identity ──────────────────────────────────────────
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LoginSession> LoginSessions => Set<LoginSession>();

    // ── Các module khác (Catalog, Circulation, Billing, Notifications...) ──────
    // Thành viên phụ trách từng module sẽ tự bổ sung DbSet tương ứng tại đây khi thêm module.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
