
using VKBot.Features.Core.Data;

namespace VKBot.Features.Host.Services
{
    public class SenderService(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                /*var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var inbox = context.Inbox.Where(i => i.Status == InboxStatus.Pendind).ToList();
                foreach (var message in inbox)
                {
                    try
                    {
                        _vk.sent(.......);
                        message.Status = InboxStatus.Sent;
                    }
                    catch(Exception ex)
                    {
                        //Логирование
                        message.CountRety++;
                        if (message.CountRety > 10)
                        {
                            message.Status = InboxStatus.Error;
                        }
                    }
                }

                if(inbox.Count > 0)
                    await context.SaveChangesAsync();

                await Task.Delay(1000, stoppingToken);*/
            }
        }
    }
}
