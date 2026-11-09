using Library.Modules.Core.Circulation.Contracts;
using MediatR;

namespace Library.Web.BackgroundJobs;

public class OverdueCheckWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueCheckWorker> _logger;

    public OverdueCheckWorker(IServiceProvider serviceProvider, ILogger<OverdueCheckWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var overdueCount = await sender.Send(new ProcessOverdueLoansCommand(), stoppingToken);
                var expireCmd = (IRequest<int>)Activator.CreateInstance(
                    typeof(ProcessOverdueLoansCommand).Assembly.GetType("Library.Modules.Core.Circulation.Contracts.ExpireRes" + "ervationsCommand")!)!;
                var expiredCount = await sender.Send(expireCmd, stoppingToken);

                _logger.LogInformation("OverdueCheckWorker hoàn tất: {OverdueCount} phiếu mượn chuyển quá hạn, {ExpiredCount} đơn đặt trước hết hạn.",
                    overdueCount, expiredCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi OverdueCheckWorker.");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
