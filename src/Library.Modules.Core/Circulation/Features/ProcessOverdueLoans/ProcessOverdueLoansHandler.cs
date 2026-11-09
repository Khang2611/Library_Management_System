using Library.Modules.Core.Circulation.Contracts;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using Library.Modules.Core.Shared.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Library.Modules.Core.Circulation.Features.ProcessOverdueLoans;

public sealed class ProcessOverdueLoansHandler : IRequestHandler<ProcessOverdueLoansCommand, int>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessOverdueLoansHandler> _logger;

    public ProcessOverdueLoansHandler(AppDbContext db, IPublisher publisher, ILogger<ProcessOverdueLoansHandler> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<int> Handle(ProcessOverdueLoansCommand request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var overdueDetails = await _db.LoanSlipDetails
            .Include(d => d.LoanSlip)
            .Where(d => d.Status == LoanDetailStatus.Borrowing &&
                        d.LoanSlip.DueDate < today &&
                        d.LoanSlip.Status != LoanStatus.Overdue)
            .ToListAsync(ct);

        if (overdueDetails.Count == 0)
            return 0;

        var modifiedLoanSlips = new HashSet<long>();

        foreach (var detail in overdueDetails)
        {
            detail.LoanSlip.Status = LoanStatus.Overdue;
            detail.LoanSlip.UpdatedAt = DateTime.UtcNow;
            modifiedLoanSlips.Add(detail.LoanSlip.Id);
        }

        await _db.SaveChangesAsync(ct);

        foreach (var detail in overdueDetails)
        {
            try
            {
                await _publisher.Publish(new LoanOverdueEvent(
                    detail.Id,
                    detail.LoanSlip.AccountId,
                    detail.BookCopyId,
                    detail.LoanSlip.DueDate), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi publish LoanOverdueEvent cho chi tiết phiếu mượn {DetailId}", detail.Id);
            }
        }

        return modifiedLoanSlips.Count;
    }
}

