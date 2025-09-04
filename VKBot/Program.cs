using Microsoft.EntityFrameworkCore;
using Serilog;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Infrastructure;
using VKBot.Features.Host.Infrastructure;
using VKBot.Features.VK.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

// Features
builder.Services.AddVkFeature();
builder.Services.AddCoreFeature(builder.Configuration);
builder.Services.AddHostFeature();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
