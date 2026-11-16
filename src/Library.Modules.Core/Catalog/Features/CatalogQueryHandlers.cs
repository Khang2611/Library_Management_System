using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Catalog.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Catalog.Features;

public sealed class CatalogQueryHandlers :
    IRequestHandler<GetActiveBookCopiesCountQuery, int>,
    IRequestHandler<GetActiveBookTitlesCountQuery, int>,
    IRequestHandler<SearchCopyIdsByBarcodeQuery, IReadOnlyList<int>>
{
    private readonly CatalogDbContext _db;

    public CatalogQueryHandlers(CatalogDbContext db) => _db = db;

    public async Task<int> Handle(GetActiveBookCopiesCountQuery request, CancellationToken ct)
    {
        return await _db.BookCopies
            .AsNoTracking()
            .CountAsync(c => c.Status != CopyStatus.Lost && c.Status != CopyStatus.Inactive, ct);
    }

    public async Task<int> Handle(GetActiveBookTitlesCountQuery request, CancellationToken ct)
    {
        return await _db.Books
            .AsNoTracking()
            .CountAsync(b => b.Status == BookStatus.Active, ct);
    }

    public async Task<IReadOnlyList<int>> Handle(SearchCopyIdsByBarcodeQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return [];

        return await _db.BookCopies
            .AsNoTracking()
            .Where(c => c.Barcode.Contains(request.Keyword))
            .Select(c => c.Id)
            .ToListAsync(ct);
    }
}

