namespace WebSockets.NetCore
{
	public enum WebSocketMessageType
	{
		Continuation = 0,
		Text = 1,
		Binary = 2,
		Close = 8,
		Ping = 9,
		Pong = 10,
		Mask = 0x0F
	}
}
