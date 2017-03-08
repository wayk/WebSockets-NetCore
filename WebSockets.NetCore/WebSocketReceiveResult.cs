namespace WebSockets.NetCore
{
	internal class WebSocketReceiveResult
	{
		public int Count { get; private set; }
		public bool EndOfMessage { get; private set; }
		public WebSocketMessageType MessageType { get; private set; }

		public WebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
		{
			Count = count;
			EndOfMessage = endOfMessage;
			MessageType = messageType;
		}
		
		public override string ToString()
		{
			return $"Count={Count}, EndOfMessage={EndOfMessage}, MessageType={MessageType}";
		}
	}
}
