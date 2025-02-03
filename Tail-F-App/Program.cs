using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Tail_F_App;

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

async Task HandleWebSocketConnection(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result;

    do
    {
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"Received: {receivedMessage}");

        var serverMessage = Encoding.UTF8.GetBytes($"Echo: {receivedMessage}");
        await webSocket.SendAsync(new ArraySegment<byte>(serverMessage), result.MessageType, result.EndOfMessage, CancellationToken.None);

    } while (!result.CloseStatus.HasValue);

    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}
