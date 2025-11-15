using UnstoppableService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "UnstoppableService";
});
builder.Services.AddHostedService<UnstoppableService>();

var host = builder.Build();
host.Run();
