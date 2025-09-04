using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Data;

namespace VKBot.Features.Host.Services
{
    public class ReceiverService(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var reddis = scope.ServiceProvider.GetRequiredService<ISessionService>();

        }
    }
}
