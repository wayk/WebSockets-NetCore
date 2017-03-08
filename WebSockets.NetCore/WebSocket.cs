using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSockets.NetCore
{
	public class WebSocket : IDisposable
	{
		private const byte finMask = 0x80;
		private const byte opCodeMask = 0x0F;
		private const byte maskMask = 0x80;
		private const byte payloadLengthMask = 0x7F;

		public delegate void WebSocketEventHandler(WebSocket webSocket);
		public delegate void ErrorEventHandler(WebSocket webSocket, string message);
		public delegate void MessageEventHandler(WebSocket webSocket, WebSocketMessageType messageType, byte[] message);

		private static readonly Random random = new Random();

		private TcpClient tcpClient;
		private int remainingMessageBytes;
		private Task receivingTask;
		private CancellationTokenSource cancellationTokenSource;
		private WebSocketReadState readState;
		private WebSocketMessageType currentMessageType;

		private Stream Stream => (Stream)SslStream ?? tcpClient.GetStream();

		internal bool IsMasked { get; set; }
		internal bool IsSecure { get; set; }
		internal SslStream SslStream { get; set; }

		public WebSocketState State { get; private set; }

		public event WebSocketEventHandler OnOpen;
		public event MessageEventHandler OnMessage;
		public event WebSocketEventHandler OnClose;
		public event ErrorEventHandler OnError;

		internal WebSocket(TcpClient tcpClient)
			: this()
		{
			this.tcpClient = tcpClient;
			State = WebSocketState.Open;
		}
		
		public WebSocket()
		{
			cancellationTokenSource = new CancellationTokenSource();

			IsMasked = true;
		}

		~WebSocket()
		{
			Dispose();
		}

		public async Task<bool> ConnectAsync(string url)
		{
			if (State != WebSocketState.None)
			{
				throw new InvalidOperationException("WebSocket is already connected or has been closed");
			}

			Uri uri = new Uri(url);

			if (uri.Scheme != "ws" && uri.Scheme != "wss")
			{
				throw new InvalidOperationException($"URI scheme \"{uri.Scheme}\" is not valid for WebSockets");
			}

			IsSecure = uri.Scheme == "wss";
			tcpClient = new TcpClient();

			try
			{
				int port = uri.Port != 0 ? uri.Port : (uri.Scheme == "ws" ? 80 : 443);

				await tcpClient.ConnectAsync(uri.DnsSafeHost, port);

				if (IsSecure)
				{
					SslStream = new SslStream(Stream, false, new RemoteCertificateValidationCallback((sender, cert, chain, errors) => errors != SslPolicyErrors.RemoteCertificateChainErrors));
					await SslStream.AuthenticateAsClientAsync(uri.DnsSafeHost);
				}

				HttpRequestHeader requestHeader = new HttpRequestHeader();

				requestHeader.Method = "GET";
				requestHeader.Path = uri.LocalPath;
				requestHeader[HttpHeaderFieldNames.Connection] = "Upgrade";
				requestHeader[HttpHeaderFieldNames.Upgrade] = "websocket";
				requestHeader[HttpHeaderFieldNames.SecWebSocketVersion] = "13";
				requestHeader[HttpHeaderFieldNames.SecWebSocketKey] = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

				requestHeader.Write(Stream);

				HttpResponseHeader responseHeader = new HttpResponseHeader();
				responseHeader.Read(Stream);

				if (responseHeader.Code != HttpStatusCode.SwitchingProtocols)
				{
					return false;
				}

				State = WebSocketState.Open;
				StartReceiving();

				OnOpen?.Invoke(this);

				return true;
			}
			catch (Exception ex)
			{
				OnError?.Invoke(this, ex.Message);
				return false;
			}
		}

		public Task SendAsync(string text)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(text);
			return SendCoreAsync(buffer, 0, buffer.Length, WebSocketMessageType.Text, true);
		}

		public Task SendAsync(byte[] buffer, int offset, int count)
		{
			return SendCoreAsync(buffer, offset, count, WebSocketMessageType.Binary, true);
		}

		public async Task CloseAsync()
		{
			await CloseCoreAsync(true);
		}

		public void Dispose()
		{
			CloseSocket();

			if (cancellationTokenSource != null)
			{
				cancellationTokenSource.Dispose();
				cancellationTokenSource = null;
			}

			if (SslStream != null)
			{
				SslStream.Dispose();
				SslStream = null;
			}
		}

		internal void StartReceiving()
		{
			receivingTask = Task.Run(delegate
			{
				try
				{
					ReceiveAsync().Wait();
				}
				catch (Exception ex)
				{
					OnError?.Invoke(this, ex.Message);
				}
				finally
				{
					CloseSocket();
				}
			}, cancellationTokenSource.Token);
		}

		private async Task ReceiveAsync()
		{
			byte[] buffer = new byte[4096];
			MemoryStream currentMessage = new MemoryStream();

			while (State == WebSocketState.Open && !cancellationTokenSource.IsCancellationRequested)
			{
				WebSocketReceiveResult result = await ReceiveAsync(buffer, cancellationTokenSource.Token);

				if (result == null)
				{
					break;
				}

				if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
				{
					currentMessageType = result.MessageType;
				}

				//TODO: Check if the last frame before the first continuation frame is marked with the FIN field
				currentMessage.Write(buffer, 0, result.Count);

				if (result.EndOfMessage)
				{
					switch (result.MessageType)
					{
						case WebSocketMessageType.Continuation:
						case WebSocketMessageType.Text:
						case WebSocketMessageType.Binary:
							{
								byte[] message = currentMessage.ToArray();
								currentMessage.Seek(0, SeekOrigin.Begin);
								currentMessage.SetLength(0);
								OnMessage?.Invoke(this, currentMessageType, message);
							}
							break;
						case WebSocketMessageType.Close:
							await CloseCoreAsync(false);
							break;
						case WebSocketMessageType.Ping:
							await SendAsync(WebSocketMessageType.Pong);
							break;
						case WebSocketMessageType.Pong:
							//TODO: Update keepAlive timer if not already done
							break;
					}
				}
			}
		}

		private async Task<WebSocketReceiveResult> ReceiveAsync(byte[] buffer, CancellationToken cancellationToken)
		{
			int bytesRead = await ReceiveCoreAsync(buffer);

			if (bytesRead == -1 || !(readState.Masked ^ IsMasked))
			{
				CloseSocket();
				return null;
			}
			
			if (readState.Masked)
			{
				Xor(buffer, readState.Mask);
			}

			return new WebSocketReceiveResult(bytesRead, (WebSocketMessageType)readState.OpCode, readState.Fin && remainingMessageBytes == 0);
		}

		private async Task<int> ReceiveCoreAsync(byte[] buffer)
		{
			int bytesToRead;

			if (remainingMessageBytes > 0)
			{
				bytesToRead = Math.Min(buffer.Length, remainingMessageBytes);
				remainingMessageBytes = remainingMessageBytes - bytesToRead;

				return await ReadBytes(buffer, 0, bytesToRead);
			}

			if (await ReadBytes(buffer, 0, 2) <= 0) return -1;

			readState.Fin = (buffer[0] & finMask) != 0;
			readState.OpCode = (byte)(buffer[0] & opCodeMask);
			readState.Masked = (buffer[1] & maskMask) != 0;
			readState.PayloadLength = (byte)(buffer[1] & payloadLengthMask);

			if (readState.PayloadLength == 126)
			{
				if (await ReadBytes(buffer, 0, 2) <= 0) return -1;

				readState.PayloadLength = EndianHelper.Swap(BitConverter.ToUInt16(buffer, 0));
			}
			else if (readState.PayloadLength == 127)
			{
				if (await ReadBytes(buffer, 0, 8) <= 0) return -1;

				readState.PayloadLength = (int)EndianHelper.Swap(BitConverter.ToUInt64(buffer, 0));
			}

			if (readState.Masked)
			{
				if (await ReadBytes(buffer, 0, 4) <= 0) return -1;

				readState.Mask = BitConverter.ToUInt32(buffer, 0);
			}

			bytesToRead = Math.Min(buffer.Length, readState.PayloadLength);

			remainingMessageBytes = readState.PayloadLength - bytesToRead;

			return await ReadBytes(buffer, 0, bytesToRead);
		}

		private async Task<int> ReadBytes(byte[] buffer, int offset, int count)
		{
			int totalBytesRead = 0;

			while (totalBytesRead < count)
			{
				int bytesRead = await Stream.ReadAsync(buffer, offset + totalBytesRead, count - totalBytesRead);
				totalBytesRead += bytesRead;

				if (bytesRead == 0)
				{
					return 0;
				}
			}

			return totalBytesRead;
		}

		private Task SendAsync(WebSocketMessageType message)
		{
			return SendCoreAsync(new byte[0], 0, 0, message, true);
		}

		private async Task SendCoreAsync(byte[] buffer, int offset, int count, WebSocketMessageType messageType, bool endOfMessage)
		{
			if (State != WebSocketState.Open)
			{
				return;
			}

			try
			{
				byte payloadLength = 0;
				int extendedPayloadLength = 0;

				BinaryWriter writer = new BinaryWriter(Stream);

				if (buffer.Length > ushort.MaxValue)
				{
					extendedPayloadLength = buffer.Length;
					payloadLength = 127;
				}
				else if (buffer.Length > 125)
				{
					extendedPayloadLength = buffer.Length;
					payloadLength = 126;
				}
				else
				{
					payloadLength = (byte)buffer.Length;
				}

				byte opCode = (byte)((byte)messageType | (endOfMessage ? finMask : 0));
				payloadLength |= IsMasked ? maskMask : (byte)0;
				uint mask = IsMasked ? (uint)random.Next() : 0;

				writer.Write(opCode);
				writer.Write(payloadLength);

				if (payloadLength == 127)
				{
					writer.Write((ushort)extendedPayloadLength);
				}
				else if (payloadLength == 126)
				{
					writer.Write((ulong)extendedPayloadLength);
				}

				if (IsMasked)
				{
					Xor(buffer, mask);

					writer.Write(mask);
				}

				await Stream.WriteAsync(buffer, offset, count);
				await Stream.FlushAsync();
			}
			catch
			{
				CloseSocket();
			}
		}

		private async Task CloseCoreAsync(bool initiate)
		{
			if (State != WebSocketState.Open)
			{
				return;
			}

			State = WebSocketState.CloseSent;

			cancellationTokenSource.Cancel();

			CancellationTokenSource closeCancellationTokenSource = new CancellationTokenSource(1000);

			await SendAsync(WebSocketMessageType.Close);

			if (initiate)
			{
				byte[] buffer = new byte[128];
				await ReceiveAsync(buffer, closeCancellationTokenSource.Token);
			}

			CloseSocket();
		}

		private void CloseSocket()
		{
			State = WebSocketState.Closed;

			if (tcpClient != null)
			{
				tcpClient.Dispose();
				tcpClient = null;

				OnClose?.Invoke(this);
			}
		}

		private static void Xor(byte[] buffer, uint mask)
		{
			byte[] keyBytes = BitConverter.GetBytes(mask);

			for (int i = 0; i < buffer.Length; i++)
			{
				buffer[i] = (byte)(buffer[i] ^ keyBytes[i % 4]);
			}
		}

		private struct WebSocketReadState
		{
			public bool Fin;
			public byte OpCode;
			public bool Masked;
			public int PayloadLength;
			public uint Mask;
		}
	}
}