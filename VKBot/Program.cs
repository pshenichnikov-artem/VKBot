using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Infrastructure;
using VKBot.Features.Host.BackgroundServices;
using VKBot.Features.Host.Infrastructure;
using VKBot.Features.Host.Services;
using VKBot.Features.VK.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}")
    .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

var logger = Log.ForContext<Program>();

try
{
    logger.Information("Запуск приложения");
    
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging(builder => builder.AddSerilog());

    // Features
    services.AddVkFeature();
    services.AddHostFeature();
    services.AddCoreFeature(configuration);

    var serviceProvider = services.BuildServiceProvider();

    using (var scope = serviceProvider.CreateScope())
    {
        logger.Information("Выполнение миграций БД");
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        logger.Information("Миграции БД завершены");
    }

    logger.Information("Запуск сервисов");
    var longPollService = serviceProvider.GetRequiredService<VkLongPollBackgroundService>();
    var messageDeliveryService = serviceProvider.GetRequiredService<MessageDeliveryBackgroundService>();
    var adminSyncService = serviceProvider.GetRequiredService<AdminSyncBackgroundService>();
    var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (_, e) =>
    {
        logger.Information("Получен сигнал остановки");
        e.Cancel = true;
        cts.Cancel();
    };

    var longPollTask = longPollService.ExecuteAsync(cts.Token);
    var messageDeliveryTask = messageDeliveryService.StartAsync(cts.Token);
    var adminSyncTask = adminSyncService.StartAsync(cts.Token);
    
    await Task.WhenAll(longPollTask, messageDeliveryTask, adminSyncTask);
}
catch(Exception ex)
{
    logger.Fatal(ex, "Критическая ошибка приложения");
}
finally
{
    logger.Information("Приложение остановлено");
    Log.CloseAndFlush();
}