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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── MediatR — scan toàn bộ assembly của Library.Modules.Core ──────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(AppDbContext).Assembly));

// ── FluentValidation — auto-register tất cả validators từ Core assembly ───────
builder.Services.AddValidatorsFromAssembly(typeof(AppDbContext).Assembly);

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
// Các thành viên có thể đăng ký HostedService / Background Service tại đây nếu cần.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<Library.Web.Middleware.GlobalExceptionMiddleware>();
app.UseCors();
app.UseHttpsRedirection();
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

// ── Các module khác (Catalog, Circulation, Billing, Notifications, AI,...) ───
// Các thành viên phụ trách từng module sẽ tự đăng ký Endpoint tại đây:
// ── Catalog ───────────────────────────────────────────────────────────────────
AddBookEndpoint.Map(app);
Library.Modules.Core.Catalog.Features.UpdateBook.UpdateBookEndpoint.Map(app);
Library.Modules.Core.Catalog.Features.GetBookDetails.GetBookDetailsEndpoint.Map(app);
SearchBookEndpoint.Map(app);
ManageCopiesEndpoint.Map(app);
Library.Modules.Core.Catalog.Features.ManageAuthors.ManageAuthorsEndpoint.Map(app);
Library.Modules.Core.Catalog.Features.ManageCategories.ManageCategoriesEndpoint.Map(app);

// ── Database Schema & Seed Initializer ────────────────────────────────────────
await Library.Web.DbInitializer.InitializeAsync(app);

app.Run();
