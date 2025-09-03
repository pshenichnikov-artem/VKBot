using Serilog;
using VKBot.Features.VK.Infrastructure;
using VKBot.Features.Core.Infrastructure;
using VKBot.Features.Host.Infrastructure;
using VKBot.Features.Core.Data;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Features
builder.Services.AddVkFeature();
builder.Services.AddCoreFeature(builder.Configuration);
builder.Services.AddHostFeature();

var app = builder.Build();

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
