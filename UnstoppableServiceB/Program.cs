using UnstoppableServiceB;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "UnstoppableServiceB";
});
builder.Services.AddHostedService<UnstoppableServiceB>();

var host = builder.Build();
host.Run();
