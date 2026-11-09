using FluentValidation;
using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Identity.Contracts;
using Library.Modules.Core.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.BorrowBook;

public sealed class BorrowBookHandler : IRequestHandler<BorrowBookRequest, ApiResponse<long>>
{
    private readonly AppDbContext _db;
    private readonly IValidator<BorrowBookRequest> _validator;
    private readonly ISender _sender;

    public BorrowBookHandler(AppDbContext db, IValidator<BorrowBookRequest> validator, ISender sender)
    {
        _db = db;
        _validator = validator;
        _sender = sender;
    }

    public async Task<ApiResponse<long>> Handle(BorrowBookRequest request, CancellationToken ct)
    {
        // ── Validate input ────────────────────────────────────────────────────
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ApiResponse<long>.Fail(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        // ── Verify account exists and is Active ───────────────────────────────
        var account = await _sender.Send(new GetAccountSummaryQuery(request.AccountId), ct);
        if (account == null)
            return ApiResponse<long>.Fail($"Tài khoản với Id = {request.AccountId} không tồn tại.");

        if (!account.IsActive)
            return ApiResponse<long>.Fail("Tài khoản đang bị khóa hoặc ngưng hoạt động, không thể mượn sách.");

        // ── Check book copies availability ────────────────────────────────────
        var distinctCopyIds = request.BookCopyIds.Distinct().ToList();
        var copyMap = await _sender.Send(new GetCopyInfosQuery(distinctCopyIds), ct);

        if (copyMap.Count != distinctCopyIds.Count)
            return ApiResponse<long>.Fail("Một hoặc nhiều bản sao sách không tồn tại.");

        var copies = copyMap.Values.ToList();

        foreach (var copy in copies)
        {
            if (!copy.BookIsActive)
                return ApiResponse<long>.Fail($"Sách '{copy.BookTitle}' hiện đang ngưng phục vụ.");
            if (copy.Condition == CopyCondition.Lost)
                return ApiResponse<long>.Fail($"Bản sao {copy.Barcode} đã bị mất.");
            if (copy.Condition == CopyCondition.MajorDamage)
                return ApiResponse<long>.Fail($"Bản sao {copy.Barcode} bị hư hỏng nặng, không thể mượn.");
        }

        // Fetch active reservations for these copies
        var activeReservations = await _db.Reservations
            .Where(r => r.BookCopyId.HasValue && 
                        distinctCopyIds.Contains(r.BookCopyId.Value) && 
                        r.Status == ReservationStatus.Ready)
            .ToListAsync(ct);

        var unavailableCopies = new List<int>();
        var reservationsToComplete = new List<Reservation>();

        foreach (var copy in copies)
        {
            var copyReservation = activeReservations.FirstOrDefault(r => r.BookCopyId == copy.CopyId);
            if (copy.Status == CopyStatus.Available)
            {
                if (copyReservation != null && copyReservation.AccountId != request.AccountId)
                    unavailableCopies.Add(copy.CopyId);
                else if (copyReservation != null && copyReservation.AccountId == request.AccountId)
                    reservationsToComplete.Add(copyReservation);
            }
            else if (copy.Status == CopyStatus.Reserved)
            {
                if (copyReservation != null && copyReservation.AccountId == request.AccountId)
                    reservationsToComplete.Add(copyReservation);
                else
                    unavailableCopies.Add(copy.CopyId);
            }
            else
            {
                unavailableCopies.Add(copy.CopyId);
            }
        }

        if (unavailableCopies.Any())
        {
            var unavailableIds = string.Join(", ", unavailableCopies);
            return ApiResponse<long>.Fail($"Các bản sao sách sau không sẵn sàng để mượn: {unavailableIds}");
        }

        // ── Create LoanSlip & Details ─────────────────────────────────────────
        var loanSlip = new LoanSlip
        {
            AccountId = request.AccountId,
            LibrarianId = request.LibrarianId,
            DueDate = request.DueDate,
            Notes = request.Notes,
            Status = LoanStatus.Borrowing
        };

        foreach (var copy in copies)
        {
            var copyReservation = activeReservations.FirstOrDefault(r => r.BookCopyId == copy.CopyId);
            var expectedStatus = (copy.Status == CopyStatus.Reserved && copyReservation?.AccountId == request.AccountId)
                ? CopyStatus.Reserved
                : CopyStatus.Available;

            var updated = await _sender.Send(new SetCopyStatusCommand(copy.CopyId, expectedStatus, CopyStatus.Borrowed), ct);
            if (!updated)
            {
                return ApiResponse<long>.Fail($"Bản sao sách không còn sẵn sàng.");
            }

            // Add detail
            loanSlip.Details.Add(new LoanSlipDetail
            {
                BookCopyId = copy.CopyId,
                Status = LoanDetailStatus.Borrowing
            });
        }

        // Mark matched reservations as completed
        foreach (var res in reservationsToComplete)
        {
            res.Status = ReservationStatus.Completed;
        }

        _db.LoanSlips.Add(loanSlip);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<long>.Fail("Bản sao sách vừa được cập nhật bởi giao dịch khác. Vui lòng thử lại.");
        }

        return ApiResponse<long>.Ok(loanSlip.Id);
    }
}
