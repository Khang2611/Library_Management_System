using System.Text;
using FluentValidation;
using Library.Modules.Core.Identity.Services;
using Library.Modules.Core.Shared;
using Library.Modules.Core.Identity.Features.Register;
using Library.Modules.Core.Identity.Features.Login;
using Library.Modules.Core.Identity.Features.UpdateProfile;
using Library.Modules.Core.Catalog.Features.AddBook;
using Library.Modules.Core.Catalog.Features.SearchBook;
using Library.Modules.Core.Catalog.Features.ManageCopies;
using Library.Modules.Core.Billing.Features.IssueFine;
using Library.Modules.Core.Billing.Features.PayFine;
using Library.Modules.Core.Billing.Data;
using Library.Modules.Core.Catalog.Data;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Notifications.Data;
using Library.Modules.Core.Search.Data;
using Library.Modules.Core.Shared.Persistence;
using Library.Web;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Database & Persistence ───────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddSharedPersistence(connectionString);
builder.Services.AddModuleDbContext<IdentityDbContext>();
builder.Services.AddModuleDbContext<CatalogDbContext>();
builder.Services.AddModuleDbContext<CirculationDbContext>();
builder.Services.AddModuleDbContext<BillingDbContext>();
builder.Services.AddModuleDbContext<NotificationsDbContext>();
builder.Services.AddModuleDbContext<SearchDbContext>();

// ── MediatR — scan toàn bộ assembly của Library.Modules.Core ──────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IdentityDbContext).Assembly);
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
});

// ── FluentValidation — auto-register tất cả validators từ Core assembly ───────
builder.Services.AddValidatorsFromAssembly(typeof(IdentityDbContext).Assembly);

// ── Identity Services ─────────────────────────────────────────────────────────
builder.Services.AddSingleton<JwtTokenService>();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Librarian", "Admin"));
    options.AddPolicy("LibrarianOnly", policy => policy.RequireRole("Librarian", "Admin"));
});

// ── API infra ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS — cho phép Frontend gọi API ─────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Background Jobs ───────────────────────────────────────────────────────────
builder.Services.AddHostedService<Library.Web.BackgroundJobs.OverdueCheckWorker>();

builder.Services.AddScoped<Library.Modules.Core.Notifications.Services.IEmailService, Library.Modules.Core.Notifications.Services.EmailService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<Library.Web.Middleware.GlobalExceptionMiddleware>();
app.UseCors();
// app.UseHttpsRedirection(); // Tắt HTTPS redirect khi chạy HTTP (Docker cổng 8082 / Local 5217) để tránh lỗi CORS / ERR_SSL_PROTOCOL_ERROR trên Frontend
app.UseAuthentication();
app.UseAuthorization();

// ── Identity ──────────────────────────────────────────────────────────────────
RegisterEndpoint.Map(app);
LoginEndpoint.Map(app);
UpdateProfileEndpoint.Map(app);
Library.Modules.Core.Identity.Features.ManageUsers.ManageUsersEndpoint.Map(app);
Library.Modules.Core.Identity.Features.ChangePassword.ChangePasswordEndpoint.Map(app);
Library.Modules.Core.Identity.Features.RefreshToken.RefreshTokenEndpoint.Map(app);
Library.Modules.Core.Identity.Features.Logout.LogoutEndpoint.Map(app);
Library.Modules.Core.Identity.Features.GetMyProfile.GetMyProfileEndpoint.Map(app);

// ── Catalog ───────────────────────────────────────────────────────────────────
AddBookEndpoint.Map(app);
Library.Modules.Core.Catalog.Features.UpdateBook.UpdateBookEndpoint.Map(app);
Library.Modules.Core.Catalog.Features.GetBookDetails.GetBookDetailsEndpoint.Map(app);
SearchBookEndpoint.Map(app);
ManageCopiesEndpoint.Map(app);
Library.Modules.Core.Catalog.Features.ManageAuthors.ManageAuthorsEndpoint.Map(app);
Library.Modules.Core.Catalog.Features.ManageCategories.ManageCategoriesEndpoint.Map(app);

// ── Circulation ───────────────────────────────────────────────────────────────
app.MapCirculationEndpoints();

// ── Billing ───────────────────────────────────────────────────────────────────
IssueFineEndpoint.Map(app);
PayFineEndpoint.Map(app);
Library.Modules.Core.Billing.Features.GetMyFines.GetMyFinesEndpoint.Map(app);
Library.Modules.Core.Billing.Features.GetAllFines.GetAllFinesEndpoint.Map(app);

// ── Database Schema Migration & Default Admin Seeding ─────────────────────────
try
{
    var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
    var targetDb = csb.InitialCatalog;
    if (!string.IsNullOrEmpty(targetDb) && !targetDb.Equals("master", StringComparison.OrdinalIgnoreCase))
    {
        var masterCsb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        using var masterConn = new Microsoft.Data.SqlClient.SqlConnection(masterCsb.ConnectionString);
        await masterConn.OpenAsync();
        using var cmd = masterConn.CreateCommand();
        cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{targetDb}') CREATE DATABASE [{targetDb}]";
        await cmd.ExecuteNonQueryAsync();
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning("Auto-create DB check skipped: {Msg}", ex.Message);
}

using (var scope = app.Services.CreateScope())
{
    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var circulationDb = scope.ServiceProvider.GetRequiredService<CirculationDbContext>();
    var billingDb = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    var searchDb = scope.ServiceProvider.GetRequiredService<SearchDbContext>();

    try
    {
        await identityDb.Database.MigrateAsync();
        await catalogDb.Database.MigrateAsync();
        await circulationDb.Database.MigrateAsync();
        await billingDb.Database.MigrateAsync();
        await notificationsDb.Database.MigrateAsync();
        await searchDb.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Lỗi khi chạy Migration: {Msg}", ex.Message);
        throw;
    }

    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
    await sender.Send(new Library.Modules.Core.Identity.Contracts.SeedDefaultAdminCommand());
}

// ── Database Initializer ──────────────────────────────────────────────────────
await Library.Web.DbInitializer.InitializeAsync(app);

app.Run();
