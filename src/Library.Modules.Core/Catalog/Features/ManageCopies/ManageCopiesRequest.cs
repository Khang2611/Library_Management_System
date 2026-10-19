using Library.Modules.Core.Catalog.Data;
using Library.Modules.Core.Shared;
using MediatR;

namespace Library.Modules.Core.Catalog.Features.ManageCopies;

/// <summary>Adds a new physical copy (BanSaoSach) to an existing book.</summary>
public record AddCopyRequest(
    int BookId,
    string Barcode,
    string? Location,
    DateOnly? AcquiredDate,
    CopyCondition Condition = CopyCondition.Good
) : IRequest<ApiResponse<int>>;
