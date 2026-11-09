using Library.Modules.Core.Identity.Contracts;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Library.Modules.Core.Identity.Features.SeedDefaultAdmin;

public sealed class SeedDefaultAdminHandler : IRequestHandler<SeedDefaultAdminCommand, bool>
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public SeedDefaultAdminHandler(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<bool> Handle(SeedDefaultAdminCommand request, CancellationToken ct)
    {
        var hasAdmin = await _db.Accounts.AnyAsync(a => a.Role == AccountRole.Admin, ct);
        if (hasAdmin)
            return false;

        var username = (_config["AdminSeed:Username"] ?? "admin").Trim().ToLowerInvariant();
        var password = _config["AdminSeed:Password"] ?? "Admin@123";
        var email = (_config["AdminSeed:Email"] ?? "admin@library.edu.vn").Trim().ToLowerInvariant();
        var fullName = (_config["AdminSeed:FullName"] ?? "Quản Trị Viên Hệ Thống").Trim();

        var admin = new Account
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            Email = email,
            PhoneNumber = "0988888888",
            Role = AccountRole.Admin,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(admin);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

