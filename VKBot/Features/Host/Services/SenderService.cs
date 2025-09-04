
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Data;

namespace VKBot.Features.Host.Services
{
    public class SenderService(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                //TODO оптимизировать все запросы в redis и убрать ненужные inbox
                var reddis = scope.ServiceProvider.GetRequiredService<ISessionService>();

                var inbox = context.Inbox.Where(i => i.Status == InboxStatus.Pendind).ToList();
                foreach (var message in inbox)
                {
                    try
                    {
                        var session = reddis.GetSessionAsync(message.UserId);
                        if (session != null)
                        {
                            continue;
                        }

                        _vk.sent(.......);
                        message.Status = InboxStatus.Sent;
                        РЕТРАЙ СООБЩШЕНИЯ ЧЕРЕЗ ЧАС, ЕСЛИ ЕГО СКИПНУЛИ
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