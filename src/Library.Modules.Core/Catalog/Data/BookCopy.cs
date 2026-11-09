using Library.Modules.Core.Catalog.Contracts;

namespace Library.Modules.Core.Catalog.Data;

/// <summary>
/// Represents a single physical copy of a book (BanSaoSach).
/// Each copy has its own barcode, physical location, condition and availability status.
/// </summary>
public class BookCopy
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; } = default!;

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    public string Barcode { get; set; } = default!;
    public DateOnly AcquiredDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public CopyCondition Condition { get; set; } = CopyCondition.Good;
    public CopyStatus Status { get; set; } = CopyStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
