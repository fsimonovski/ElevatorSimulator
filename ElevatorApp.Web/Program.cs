using ElevatorApp.Core;
using ElevatorApp.Web.Hubs;
using ElevatorApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IElevatorTimingConfig, DefaultElevatorTimingConfig>();
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IElevatorTimingConfig>();
    return new ElevatorControlSystem(elevatorCount: 4, cfg);
});

builder.Services.AddSingleton<SimulationService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<SimulationService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapHub<ElevatorHub>("/elevatorHub");

app.Run();