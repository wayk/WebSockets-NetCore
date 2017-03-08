using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets.NetCore
{
	public class WebSocketListener : IDisposable
	{
		private TcpListener listener;
		private string[] protocols;

		public X509Certificate Certificate { get; set; }

		public WebSocketListener(IPAddress localAddress, int port, params string[] protocols)
		{
			listener = new TcpListener(localAddress, port);
			this.protocols = protocols ?? new string[0];
		}

		~WebSocketListener()
		{
			Dispose();
		}

		public void Start()
		{
			listener.Start();
		}

		public void Stop()
		{
			listener.Stop();
		}

		public async Task<WebSocket> AcceptWebSocketClientAsync()
		{
			WebSocket result = null;

			while (result == null)
			{
				TcpClient client = await listener.AcceptTcpClientAsync();

				try
				{
					result = await CreateServerWebSocketAsync(client);

					if (result == null)
					{
						client.Dispose();
					}
					else
					{
						result.StartReceiving();
					}
				}
				catch { }
			}

			return result;
		}

		private async Task<WebSocket> CreateServerWebSocketAsync(TcpClient client)
		{
			SslStream sslStream = null;

			if (Certificate != null)
			{
				try
				{
					sslStream = new SslStream(client.GetStream());
					await sslStream.AuthenticateAsServerAsync(Certificate);
				}
				catch
				{
					return null;
				}
			}

			WebSocket result = null;
			Stream stream = (Stream)sslStream ?? client.GetStream();

			HttpRequestHeader requestHeader = new HttpRequestHeader();
			requestHeader.Read(stream);

			HttpResponseHeader responseHeader = new HttpResponseHeader();
			responseHeader.Code = HttpStatusCode.BadRequest;

			if (IsValidWebSocketRequest(requestHeader))
			{
				int webSocketVersion = int.Parse(requestHeader[HttpHeaderFieldNames.SecWebSocketVersion]);
				string webSocketKey = requestHeader[HttpHeaderFieldNames.SecWebSocketKey];

				if (requestHeader.Fields[HttpHeaderFieldNames.SecWebSocketProtocol] != null)
				{
					string[] protocols = requestHeader.Fields[HttpHeaderFieldNames.SecWebSocketProtocol].Split(',').Where(s => s != null).Select(s => s.Trim()).ToArray();

					string protocol = protocols.FirstOrDefault(p => protocols.Contains(p));

					if (protocol != null)
					{
						responseHeader.Fields[HttpHeaderFieldNames.SecWebSocketProtocol] = protocol;
					}
				}

				responseHeader.Code = HttpStatusCode.SwitchingProtocols;
				responseHeader.Fields[HttpHeaderFieldNames.Connection] = "Upgrade";
				responseHeader.Fields[HttpHeaderFieldNames.Upgrade] = "websocket";
				responseHeader.Fields[HttpHeaderFieldNames.SecWebSocketAccept] = CreateWebSocketKey(requestHeader[HttpHeaderFieldNames.SecWebSocketKey]);

				result = new WebSocket(client);

				if (sslStream != null)
				{
					result.IsSecure = true;
					result.SslStream = sslStream;
				}
			}

			responseHeader.Write(stream);
			return result;
		}

		private static string CreateWebSocketKey(string clientKey)
		{
			return Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(clientKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
		}

		private static bool IsValidWebSocketRequest(HttpRequestHeader requestHeader)
		{
			return requestHeader.Method == "GET" &&
				requestHeader[HttpHeaderFieldNames.Connection] == "Upgrade" &&
				requestHeader[HttpHeaderFieldNames.Upgrade] == "websocket" &&
				requestHeader[HttpHeaderFieldNames.SecWebSocketVersion] != null &&
				requestHeader[HttpHeaderFieldNames.SecWebSocketKey] != null;
		}

		public void Dispose()
		{
			if (listener != null)
			{
				listener.Stop();
				listener = null;
			}
		}
	}
}
