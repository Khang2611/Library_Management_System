namespace Library.Modules.Core.Catalog.Data;

/// <summary>
/// Physical 3D coordinate location for bookshelves (He Toa Do Vi Tri Ke 3D).
/// Identified by Aisle, Shelf, and RowLevel.
/// </summary>
public class Location
{
    public int Id { get; set; }
    public string Aisle { get; set; } = default!;
    public string Shelf { get; set; } = default!;
    public int RowLevel { get; set; }
    public int? MaxCapacity { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation (within Catalog context)
    public ICollection<BookCopy> Copies { get; set; } = [];
}

