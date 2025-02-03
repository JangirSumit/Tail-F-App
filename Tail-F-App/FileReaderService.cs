using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Tail_F_App
{
    public class FileReaderService
    {
        private readonly ConcurrentDictionary<string, long> _filePositions = new();

        public async Task StartAsync(WebSocket webSocket, string filePath, int lineCount)
        {
            if (!File.Exists(filePath))
            {
                await SendMessage(webSocket, "File not found.");
                return;
            }

            await PrintLastLines(webSocket, filePath, lineCount);

            _filePositions[filePath] = new FileInfo(filePath).Length;

            using var watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath)!)
            {
                Filter = Path.GetFileName(filePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };


            watcher.Changed += async (_, _) =>
            {
                await Task.Delay(500); // Avoid race conditions
                await SendNewLines(webSocket, filePath);
            };

            watcher.EnableRaisingEvents = true;

            // Send the initial last N lines
            await SendNewLines(webSocket, filePath);

            // Keep WebSocket open until the client disconnects
            while (webSocket.State == WebSocketState.Open)
            {
                await Task.Delay(1000);
            }

            watcher.EnableRaisingEvents = false; // Cleanup
        }

        private async Task SendNewLines(WebSocket webSocket, string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                long lastPosition = _filePositions.GetOrAdd(filePath, fs.Length);

                if (lastPosition > fs.Length)
                {
                    // File was truncated, restart from beginning
                    lastPosition = 0;
                }

                fs.Seek(lastPosition, SeekOrigin.Begin);
                using var reader = new StreamReader(fs);
                StringBuilder sb = new();
                string? newLine;

                while ((newLine = await reader.ReadLineAsync()) != null)
                {
                    sb.AppendLine(newLine);
                }

                if (sb.Length > 0)
                {
                    await SendMessage(webSocket, sb.ToString().TrimEnd());
                    _filePositions[filePath] = fs.Position; // Store new position
                }
            }
            catch (Exception ex)
            {
                await SendMessage(webSocket, $"Error: {ex.Message}");
            }
        }

        public async Task PrintLastLines(WebSocket webSocket, string filePath, int lineCount)
        {
            try
            {
                var lines = File.ReadLines(filePath).Reverse().Take(lineCount).Reverse();
                await SendMessage(webSocket, string.Join("\n", lines));
            }
            catch (IOException)
            {
                Console.WriteLine("File is currently being written to. Retrying...");
            }
        }



        private async Task SendMessage(WebSocket webSocket, string message)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
