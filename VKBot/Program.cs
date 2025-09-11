using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Infrastructure;
using VKBot.Features.Host.BackgroundServices;
using VKBot.Features.Host.Infrastructure;
using VKBot.Features.Host.Services;
using VKBot.Features.VK.Infrastructure;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
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
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

var longPollService = serviceProvider.GetRequiredService<VkLongPollService>();
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cts.Cancel();
};

await longPollService.StartAsync(cts.Token);