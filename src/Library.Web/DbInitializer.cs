using System.Text.Json;
using Library.Modules.Core.Catalog.Data;
using Library.Modules.Core.Identity.Data;
using Library.Modules.Core.Shared;
using Microsoft.EntityFrameworkCore;

namespace Library.Web;

public static class DbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        try
        {
            var db = services.GetRequiredService<AppDbContext>();

            // Retry connecting to DB container if it's starting up
            var retryCount = 0;
            while (retryCount < 10)
            {
                try
                {
                    logger.LogInformation("Attempting to connect to database...");
                    await db.Database.EnsureCreatedAsync();
                    logger.LogInformation("Database connected and schema verified.");
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    logger.LogWarning("Database connection failed (attempt {Count}/10): {Msg}. Retrying in 4s...", retryCount, ex.Message);
                    if (retryCount >= 10) throw;
                    await Task.Delay(4000);
                }
            }

            // 1. Seed Accounts if empty
            if (!await db.Accounts.AnyAsync())
            {
                logger.LogInformation("Seeding initial accounts (admin, librarian, reader)...");

                var admin = new Account
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Quản Trị Viên Hệ Thống",
                    Email = "admin@library.edu.vn",
                    PhoneNumber = "0988888888",
                    Role = AccountRole.Admin,
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                var librarian = new Account
                {
                    Username = "librarian",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Librarian@123"),
                    FullName = "Thủ Thư Nguyễn Văn B",
                    Email = "librarian@library.edu.vn",
                    PhoneNumber = "0977777777",
                    Role = AccountRole.Librarian,
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                var reader = new Account
                {
                    Username = "reader",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Reader@123"),
                    FullName = "Độc Giả Trần Thị C",
                    Email = "reader@library.edu.vn",
                    PhoneNumber = "0966666666",
                    Role = AccountRole.Reader,
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                db.Accounts.AddRange(admin, librarian, reader);
                await db.SaveChangesAsync();
                logger.LogInformation("Initial accounts seeded successfully.");
            }

            // 2. Seed Books from books_seed.json if empty
            if (!await db.Books.AnyAsync())
            {
                logger.LogInformation("Checking for books_seed.json...");
                var candidatePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "Catalog", "Data", "books_seed.json"),
                    Path.Combine(AppContext.BaseDirectory, "books_seed.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "books_seed.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "Library.Modules.Core", "Catalog", "Data", "books_seed.json")
                };

                var seedFile = candidatePaths.FirstOrDefault(File.Exists);
                if (seedFile != null)
                {
                    logger.LogInformation("Loading seed books from: {Path}", seedFile);
                    var json = await File.ReadAllTextAsync(seedFile);
                    using var doc = JsonDocument.Parse(json);

                    var categoryDict = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
                    var authorDict = new Dictionary<string, Author>(StringComparer.OrdinalIgnoreCase);

                    var count = 0;
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        var title = element.GetProperty("Title").GetString() ?? "Sách";
                        var authorName = element.TryGetProperty("Author", out var aProp) ? aProp.GetString() : "Tác giả khác";
                        var catName = element.TryGetProperty("Category", out var cProp) ? cProp.GetString() : "Tổng hợp";
                        var isbn = element.TryGetProperty("Isbn", out var iProp) ? iProp.GetString() : null;
                        var desc = element.TryGetProperty("Description", out var dProp) ? dProp.GetString() : null;
                        var pubYear = element.TryGetProperty("PublishedYear", out var yProp) ? (short?)yProp.GetInt16() : null;
                        var pub = element.TryGetProperty("Publisher", out var pProp) ? pProp.GetString() : null;
                        var lang = element.TryGetProperty("Language", out var lProp) ? lProp.GetString() : "Tiếng Việt";
                        var pages = element.TryGetProperty("PageCount", out var pgProp) ? (int?)pgProp.GetInt32() : null;
                        var val = element.TryGetProperty("BookValue", out var vProp) ? (decimal?)vProp.GetDecimal() : null;
                        var cover = element.TryGetProperty("CoverImageUrl", out var cvProp) && cvProp.ValueKind != JsonValueKind.Null ? cvProp.GetString() : null;

                        // Ensure Category
                        if (!string.IsNullOrWhiteSpace(catName) && !categoryDict.TryGetValue(catName, out var category))
                        {
                            category = new Category { Name = catName, Description = $"Chuyên mục sách {catName}" };
                            db.Categories.Add(category);
                            categoryDict[catName] = category;
                        }

                        // Ensure Author
                        if (!string.IsNullOrWhiteSpace(authorName) && !authorDict.TryGetValue(authorName, out var author))
                        {
                            author = new Author { Name = authorName, Biography = $"Tác giả {authorName}" };
                            db.Authors.Add(author);
                            authorDict[authorName] = author;
                        }

                        var book = new Book
                        {
                            Title = title,
                            Isbn = isbn,
                            Description = desc,
                            PublishedYear = pubYear,
                            Publisher = pub,
                            Language = lang,
                            PageCount = pages,
                            BookValue = val,
                            CoverImageUrl = cover,
                            Status = BookStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };

                        if (!string.IsNullOrWhiteSpace(catName) && categoryDict.TryGetValue(catName, out var catObj))
                        {
                            book.BookCategories.Add(new BookCategory { Category = catObj });
                        }

                        if (!string.IsNullOrWhiteSpace(authorName) && authorDict.TryGetValue(authorName, out var authObj))
                        {
                            book.BookAuthors.Add(new BookAuthor { Author = authObj });
                        }

                        // Add copies
                        if (element.TryGetProperty("Copies", out var copiesProp) && copiesProp.ValueKind == JsonValueKind.Array)
                        {
                            var shelfIndex = 1;
                            foreach (var copyElem in copiesProp.EnumerateArray())
                            {
                                var barcode = copyElem.GetProperty("Barcode").GetString() ?? Guid.NewGuid().ToString("N")[..8];
                                book.Copies.Add(new BookCopy
                                {
                                    Barcode = barcode,
                                    Location = $"Kệ A{shelfIndex}-01",
                                    AcquiredDate = DateOnly.FromDateTime(DateTime.Today),
                                    Condition = CopyCondition.Good,
                                    Status = CopyStatus.Available
                                });
                                shelfIndex++;
                            }
                        }
                        else
                        {
                            // default 2 copies
                            book.Copies.Add(new BookCopy
                            {
                                Barcode = $"LIB-{Random.Shared.Next(1000, 9999)}-01",
                                Location = "Kệ A1-01",
                                AcquiredDate = DateOnly.FromDateTime(DateTime.Today),
                                Condition = CopyCondition.Good,
                                Status = CopyStatus.Available
                            });
                        }

                        db.Books.Add(book);
                        count++;
                        if (count >= 120) break; // Seed first 120 books for speed and rich catalog
                    }

                    await db.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} books into catalog successfully.", count);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database initialization.");
        }
    }
}
