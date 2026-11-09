using FluentValidation;
using Library.Modules.Core.Catalog.Contracts;
using Library.Modules.Core.Circulation.Contracts;
using Library.Modules.Core.Circulation.Data;
using Library.Modules.Core.Shared;
using Library.Modules.Core.Shared.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Library.Modules.Core.Circulation.Features.ReportIncident;

public sealed class ReportIncidentHandler : IRequestHandler<ReportIncidentRequest, ApiResponse<long>>
{
    private readonly AppDbContext _db;
    private readonly IValidator<ReportIncidentRequest> _validator;
    private readonly IMediator _mediator;

    public ReportIncidentHandler(AppDbContext db, IValidator<ReportIncidentRequest> validator, IMediator mediator)
    {
        _db = db;
        _validator = validator;
        _mediator = mediator;
    }

    public async Task<ApiResponse<long>> Handle(ReportIncidentRequest request, CancellationToken ct)
    {
        var val = await _validator.ValidateAsync(request, ct);
        if (!val.IsValid) return ApiResponse<long>.Fail(string.Join("; ", val.Errors.Select(e => e.ErrorMessage)));

        var detail = await _db.LoanSlipDetails
            .Include(d => d.LoanSlip)
            .FirstOrDefaultAsync(d => d.Id == request.LoanSlipDetailId, ct);
        if (detail == null) return ApiResponse<long>.Fail("Chi tiết phiếu mượn không tồn tại.");

        var incident = new Incident
        {
            LoanSlipDetailId = request.LoanSlipDetailId,
            LibrarianId = request.LibrarianId,
            IncidentType = request.IncidentType,
            Severity = request.Severity,
            Description = request.Description,
            CompensationAmount = request.CompensationAmount,
            RecordedAt = DateTime.UtcNow,
            Status = IncidentStatus.Recorded
        };

        _db.Incidents.Add(incident);

        if (request.IncidentType == IncidentType.Lost)
        {
            detail.Status = LoanDetailStatus.Lost;
            await _mediator.Send(new SetCopyStatusCommand(
                detail.BookCopyId,
                null,
                CopyStatus.Lost,
                CopyCondition.Lost), ct);
        }
        else if (request.IncidentType == IncidentType.Damaged)
        {
            detail.Status = LoanDetailStatus.Damaged;
            var isSevere = request.Severity == IncidentSeverity.Major || request.Severity == IncidentSeverity.TotalLoss;
            var condition = isSevere ? CopyCondition.MajorDamage : CopyCondition.MinorDamage;
            var newStatus = isSevere ? CopyStatus.Maintenance : (CopyStatus?)null;

            await _mediator.Send(new SetCopyStatusCommand(
                detail.BookCopyId,
                null,
                newStatus,
                condition), ct);
        }

        // Check if all details for this loan slip are no longer borrowing
        var allDetails = await _db.LoanSlipDetails
            .Where(d => d.LoanSlipId == detail.LoanSlipId)
            .ToListAsync(ct);

        if (allDetails.All(d => (d.Id == detail.Id ? detail.Status : d.Status) != LoanDetailStatus.Borrowing))
        {
            detail.LoanSlip.Status = LoanStatus.Returned;
            detail.LoanSlip.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new IncidentReportedEvent(
            incident.Id,
            incident.LoanSlipDetailId,
            detail.LoanSlip.AccountId,
            incident.LibrarianId,
            incident.IncidentType,
            incident.Severity,
            incident.CompensationAmount), ct);

        return ApiResponse<long>.Ok(incident.Id);
    }
}
