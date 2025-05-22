using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using SqlToolsMcpServer.Services;    // for our SQL-monitoring service
using SqlToolsMcpServer.Tools;       

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddSingleton<ISqlServerMonitorService, SqlServerMonitorService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport() // HTTP streaming / SSE transport :contentReference[oaicite:1]{index=1}
    .WithToolsFromAssembly();

builder.Services.AddControllers();

// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();

app.MapControllers(); // Maps attribute-routed controllers

await app.RunAsync();
