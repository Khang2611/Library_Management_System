using System.ComponentModel.DataAnnotations.Schema;

namespace Library.Modules.Core.Circulation.Data;

/// <summary>
/// Loan slip header (PhieuMuon). 
/// One LoanSlip can contain multiple BookCopies via LoanSlipDetail.
/// </summary>
public class LoanSlip
{
    public long Id { get; set; }

    // Cross-context FK → Identity.Account (the borrower)
    public int AccountId { get; set; }

    // Cross-context FK → Identity.Account (librarian who processed, nullable)
    public int? LibrarianId { get; set; }

    public DateTime BorrowedAt { get; set; } = DateTime.UtcNow;
    public DateOnly DueDate { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Borrowing;
    public string? Notes { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// Calculated or runtime-only property. Actual return dates are stored on individual LoanSlipDetails.
    /// Not mapped to the database table.
    /// </summary>
    [NotMapped]
    public DateOnly? ReturnedDate { get; set; }

    // Navigation within Circulation context
    public ICollection<LoanSlipDetail> Details { get; set; } = [];
}

public enum LoanStatus { Pending, Borrowing, Returned, Overdue, Cancelled }
