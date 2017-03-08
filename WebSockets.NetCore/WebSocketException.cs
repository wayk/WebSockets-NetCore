using System;

namespace WebSockets.NetCore
{
	public class WebSocketException : Exception
	{
		public WebSocketException(string message)
			: base(message)
		{
		}
	}
}
