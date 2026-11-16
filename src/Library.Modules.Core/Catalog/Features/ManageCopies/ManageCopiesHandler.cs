using FluentValidation;
using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Catalog.Data;
using Library.Modules.Core.Circulation.Contracts;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Catalog.Features.ManageCopies;

/// <summary>Handles adding a new physical copy (BookCopy) to an existing book.</summary>
public sealed class ManageCopiesHandler : IRequestHandler<AddCopyRequest, ApiResponse<int>>
{
    private readonly CatalogDbContext _db;
    private readonly IValidator<AddCopyRequest> _validator;

    public ManageCopiesHandler(CatalogDbContext db, IValidator<AddCopyRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<ApiResponse<int>> Handle(AddCopyRequest request, CancellationToken ct)
    {
        // â”€â”€ Validate input â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<int>.Fail(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        // â”€â”€ Verify book exists â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var bookExists = await _db.Books.AnyAsync(b => b.Id == request.BookId, ct);
        if (!bookExists)
            return ApiResponse<int>.Fail($"SĂ¡ch vá»›i Id = {request.BookId} khĂ´ng tá»“n táº¡i.");

        // â”€â”€ Check barcode uniqueness â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var barcode = request.Barcode.Trim();
        if (await _db.BookCopies.AnyAsync(c => c.Barcode == barcode, ct))
            return ApiResponse<int>.Fail($"MĂ£ váº¡ch '{barcode}' Ä‘Ă£ tá»“n táº¡i trong há»‡ thá»‘ng.");

        // â”€â”€ Verify location if provided â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        int? locationId = request.LocationId;
        if (locationId.HasValue)
        {
            var locationExists = await _db.Locations.AnyAsync(l => l.Id == locationId.Value, ct);
            if (!locationExists)
                return ApiResponse<int>.Fail($"Vá»‹ trĂ­ ká»‡ vá»›i Id = {locationId.Value} khĂ´ng tá»“n táº¡i.");
        }

        // â”€â”€ Create BookCopy entity â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var copy = new BookCopy
        {
            BookId = request.BookId,
            Barcode = barcode,
            LocationId = locationId,
            AcquiredDate = request.AcquiredDate ?? DateOnly.FromDateTime(DateTime.Today),
            Condition = request.Condition,
            Status = CopyStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        _db.BookCopies.Add(copy);
        await _db.SaveChangesAsync(ct);

        return ApiResponse<int>.Ok(copy.Id);
    }
}

/// <summary>Handles retrieving all physical copies of a given book.</summary>
public sealed class GetCopiesHandler : IRequestHandler<GetCopiesRequest, ApiResponse<List<CopyDetail>>>
{
    private readonly CatalogDbContext _db;
    public GetCopiesHandler(CatalogDbContext db) => _db = db;

    public async Task<ApiResponse<List<CopyDetail>>> Handle(GetCopiesRequest request, CancellationToken ct)
    {
        // â”€â”€ Verify book exists â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (!await _db.Books.AnyAsync(b => b.Id == request.BookId, ct))
            return ApiResponse<List<CopyDetail>>.Fail($"SĂ¡ch vá»›i Id = {request.BookId} khĂ´ng tá»“n táº¡i.");

        // â”€â”€ Query copies â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var copies = await _db.BookCopies
            .Include(c => c.Location)
            .Where(c => c.BookId == request.BookId)
            .OrderBy(c => c.Barcode)
            .Select(c => new CopyDetail(
                c.Id,
                c.Barcode,
                c.Location != null ? $"{c.Location.Aisle}-{c.Location.Shelf}-{c.Location.RowLevel}" : null,
                c.AcquiredDate,
                c.Condition.ToString(),
                c.Status.ToString()
            ))
            .ToListAsync(ct);

        return ApiResponse<List<CopyDetail>>.Ok(copies);
    }
}

/// <summary>Handles updating the status of a specific book copy.</summary>
public sealed class UpdateCopyStatusHandler : IRequestHandler<UpdateCopyStatusRequest, ApiResponse<bool>>
{
    private readonly CatalogDbContext _db;
    private readonly ISender _sender;

    public UpdateCopyStatusHandler(CatalogDbContext db, ISender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateCopyStatusRequest request, CancellationToken ct)
    {
        // â”€â”€ Find the copy â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var copy = await _db.BookCopies.FindAsync([request.CopyId], ct);
        if (copy is null)
            return ApiResponse<bool>.Fail($"Báº£n sao vá»›i Id = {request.CopyId} khĂ´ng tá»“n táº¡i.");

        // â”€â”€ Validate enum value â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (!Enum.IsDefined(request.NewStatus))
            return ApiResponse<bool>.Fail("Tráº¡ng thĂ¡i báº£n sao khĂ´ng há»£p lá»‡.");

        var hasActiveLoan = await _sender.Send(new HasActiveLoanForCopyQuery(request.CopyId), ct);
        if (hasActiveLoan)
            return ApiResponse<bool>.Fail("KhĂ´ng thá»ƒ Ä‘á»•i tráº¡ng thĂ¡i báº£n sao Ä‘ang Ä‘Æ°á»£c mÆ°á»£n.");

        // â”€â”€ Update status â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        copy.Status = request.NewStatus;
        copy.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true);
    }
}
