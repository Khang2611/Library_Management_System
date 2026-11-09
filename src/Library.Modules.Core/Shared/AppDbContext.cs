using Library.Modules.Core.Billing.Data;
using Library.Modules.Core.Catalog.Data;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Notifications.Data;
using Library.Modules.Core.Search.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Shared;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Module Identity ──────────────────────────────────────────
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LoginSession> LoginSessions => Set<LoginSession>();

    // ── Catalog ──────────────────────────────────────────────────
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    // ── Circulation ──────────────────────────────────────────────
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<LoanSlip> LoanSlips => Set<LoanSlip>();
    public DbSet<LoanSlipDetail> LoanSlipDetails => Set<LoanSlipDetail>();
    public DbSet<Renewal> Renewals => Set<Renewal>();
    public DbSet<Incident> Incidents => Set<Incident>();

    // ── Billing ──────────────────────────────────────────────────
    public DbSet<Fine> Fines => Set<Fine>();
    public DbSet<FinePayment> FinePayments => Set<FinePayment>();

    // ── Notifications ────────────────────────────────────────────
    public DbSet<Notification> Notifications => Set<Notification>();

    // ── Search ───────────────────────────────────────────────────
    public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── Cross-module Foreign Keys configured centrally ──
        // Circulation -> Identity
        modelBuilder.Entity<LoanSlip>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(ls => ls.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LoanSlip>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(ls => ls.LibrarianId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Renewal>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(r => r.LibrarianId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Incident>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(i => i.LibrarianId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Reservation>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Circulation -> Catalog
        modelBuilder.Entity<LoanSlipDetail>()
            .HasOne<BookCopy>()
            .WithMany()
            .HasForeignKey(d => d.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne<Book>()
            .WithMany()
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne<BookCopy>()
            .WithMany()
            .HasForeignKey(r => r.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // ── Billing Cross-module Foreign Keys ──
        // Billing -> Identity
        modelBuilder.Entity<Fine>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(f => f.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FinePayment>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(p => p.LibrarianId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Billing -> Circulation
        modelBuilder.Entity<Fine>()
            .HasOne<LoanSlipDetail>()
            .WithMany()
            .HasForeignKey(f => f.LoanSlipDetailId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Fine>()
            .HasOne<Incident>()
            .WithMany()
            .HasForeignKey(f => f.IncidentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // ── Notifications Cross-module Foreign Keys ──
        // Notifications -> Identity
        modelBuilder.Entity<Notification>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(n => n.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Search Cross-module Foreign Keys ──
        // Search -> Identity
        modelBuilder.Entity<SearchHistory>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(sh => sh.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
