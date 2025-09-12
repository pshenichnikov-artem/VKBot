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
        
        var adminSync = scope.ServiceProvider.GetRequiredService<AdminSyncService>();
        await adminSync.StartAsync(CancellationToken.None);
    }

    logger.Information("Запуск VK LongPoll сервиса");
    var longPollService = serviceProvider.GetRequiredService<VkLongPollService>();
    var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (_, e) =>
    {
        logger.Information("Получен сигнал остановки");
        e.Cancel = true;
        cts.Cancel();
    };

    await longPollService.ExecuteAsync(cts.Token);
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