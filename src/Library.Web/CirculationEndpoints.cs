using Microsoft.AspNetCore.Routing;

namespace Library.Web;

public static class CirculationEndpoints
{
    public static IEndpointRouteBuilder MapCirculationEndpoints(this IEndpointRouteBuilder app)
    {
        Library.Modules.Core.Circulation.Features.BorrowBook.BorrowBookEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.ReturnBook.ReturnBookEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.ReserveBook.ReserveBookEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.RequestRenewal.RequestRenewalEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.ApproveRenewal.ApproveRenewalEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.GetMyLoans.GetMyLoansEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.CancelReservation.CancelReservationEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.ReportIncident.ReportIncidentEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.GetAllLoans.GetAllLoansEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.GetAllReservations.GetAllReservationsEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.GetLoanDetail.GetLoanDetailEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.GetAllRenewals.GetAllRenewalsEndpoint.Map(app);
        Library.Modules.Core.Circulation.Features.GetMyReservations.GetMyReservationsEndpoint.Map(app);

        return app;
    }
}

