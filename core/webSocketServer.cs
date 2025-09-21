using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketServer
{
	private readonly HttpListener _listener;

	public event Action<WebSocket>? OnClientConnected;
	public event Action<WebSocket, string>? OnMessageReceived;

	public WebSocketServer(string url)
	{
		_listener = new HttpListener();
		_listener.Prefixes.Add(url);
	}

	public async Task StartAsync()
	{
		_listener.Start();
		Console.WriteLine("WebSocket server started at " + string.Join(", ", _listener.Prefixes));

		while (true)
		{
			var context = await _listener.GetContextAsync();

			if (context.Request.IsWebSocketRequest)
			{
				var wsContext = await context.AcceptWebSocketAsync(null);
				var ws = wsContext.WebSocket;

				OnClientConnected?.Invoke(ws);

				_ = HandleClient(ws); // fire and forget
			}
			else
			{
				context.Response.StatusCode = 400;
				context.Response.Close();
			}
		}
	}

	private async Task HandleClient(WebSocket ws)
	{
		var buffer = new byte[1024];

		while (ws.State == WebSocketState.Open)
		{
			var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

			if (result.MessageType == WebSocketMessageType.Text)
			{
				string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
				OnMessageReceived?.Invoke(ws, message);
			}
			else if (result.MessageType == WebSocketMessageType.Close)
			{
				await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
			}
		}
	}

	public async Task SendToClient(WebSocket ws, string message)
	{
		if (ws.State == WebSocketState.Open)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(message);
			await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}
}
