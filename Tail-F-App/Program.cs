using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Tail_F_App;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<FileReaderService>();
        var app = builder.Build();
        app.UseWebSockets(); // Enable WebSockets

        app.Map("/ws", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var fileReaderService = context.RequestServices.GetRequiredService<FileReaderService>();

                await fileReaderService.StartAsync(webSocket, @"C:\Logs\chrome.adapter.log", 10);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing Connection", CancellationToken.None);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        });

        app.Run();
    }
}