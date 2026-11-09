using MediatR;

namespace Library.Modules.Core.Catalog.Contracts;

public enum CopyCondition { Good, MinorDamage, MajorDamage, Lost }
public enum CopyStatus { Available, Reserved, Borrowed, Maintenance, Lost, Inactive }
public enum BookStatus { Active, Inactive }

public record CopyInfo(
    int CopyId,
    string Barcode,
    int BookId,
    string BookTitle,
    bool BookIsActive,
    CopyCondition Condition,
    CopyStatus Status
);

public record GetCopyInfosQuery(IReadOnlyCollection<int> CopyIds)
    : IRequest<IReadOnlyDictionary<int, CopyInfo>>;

public record BookInfo(int BookId, string Title, bool IsActive);

public record GetBookInfosQuery(IReadOnlyCollection<int> BookIds)
    : IRequest<IReadOnlyDictionary<int, BookInfo>>;

public record FindAvailableCopyQuery(int BookId, int? PreferredCopyId) : IRequest<CopyInfo?>;

public record SetCopyStatusCommand(
    int CopyId,
    CopyStatus? ExpectedStatus,
    CopyStatus? NewStatus = null,
    CopyCondition? NewCondition = null
) : IRequest<bool>;
