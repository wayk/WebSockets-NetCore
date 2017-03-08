namespace WebSockets.NetCore
{
	public enum HttpStatusCode
	{
		Continue = 100,
		SwitchingProtocols = 101,
		OK = 200,
		Created = 201,
		Accepted = 202,
		NonAuthoritativeInformation = 203,
		NoContent = 204,
		ResetContent = 205,
		PartialContent = 206,
		MultipleChoices = 300,
		MovedPermanently = 301,
		Found = 302,
		SeeOther = 303,
		NotModified = 304,
		UseProxy = 305,
		TemporaryRedirect = 307,
		BadRequest = 400,
		Unauthorized = 401,
		PaymentRequired = 402,
		Forbidden = 403,
		NotFound = 404,
		MethodNotAllowed = 405,
		NotAcceptable = 406,
		ProxyAuthenticationRequired = 407,
		RequestTimeOut = 408,
		Conflict = 409,
		Gone = 410,
		LengthRequired = 411,
		PreconditionFailed = 412,
		RequestEntityTooLarge = 413,
		RequestURITooLarge = 414,
		UnsupportedMediaType = 415,
		Requestedrangenotsatisfiable = 416,
		ExpectationFailed = 417,
		InternalServerError = 500,
		NotImplemented = 501,
		BadGateway = 502,
		ServiceUnavailable = 503,
		GatewayTimeOut = 504,
		HTTPVersionnotsupported = 505
	}

	public static class HttpStatusCodeExtensions
	{
		public static string GetReason(this HttpStatusCode code)
		{
			switch (code)
			{
				case HttpStatusCode.SwitchingProtocols: return "Switching Protocols";
				case HttpStatusCode.NonAuthoritativeInformation: return "Non-Authoritative Information";
				case HttpStatusCode.NoContent: return "No Content";
				case HttpStatusCode.ResetContent: return "Reset Content";
				case HttpStatusCode.PartialContent: return "Partial Content";
				case HttpStatusCode.MultipleChoices: return "Multiple Choices";
				case HttpStatusCode.MovedPermanently: return "Moved Permanently";
				case HttpStatusCode.SeeOther: return "See Other";
				case HttpStatusCode.NotModified: return "Not Modified";
				case HttpStatusCode.UseProxy: return "Use Proxy";
				case HttpStatusCode.TemporaryRedirect: return "Temporary Redirect";
				case HttpStatusCode.BadRequest: return "Bad Request";
				case HttpStatusCode.PaymentRequired: return "Payment Required";
				case HttpStatusCode.NotFound: return "Not Found";
				case HttpStatusCode.MethodNotAllowed: return "Method Not Allowed";
				case HttpStatusCode.NotAcceptable: return "Not Acceptable";
				case HttpStatusCode.ProxyAuthenticationRequired: return "Proxy Authentication Required";
				case HttpStatusCode.RequestTimeOut: return "Request Time-out";
				case HttpStatusCode.LengthRequired: return "Length Required";
				case HttpStatusCode.PreconditionFailed: return "Precondition Failed";
				case HttpStatusCode.RequestEntityTooLarge: return "Request Entity Too Large";
				case HttpStatusCode.RequestURITooLarge: return "Request-URI Too Large";
				case HttpStatusCode.UnsupportedMediaType: return "Unsupported Media Type";
				case HttpStatusCode.Requestedrangenotsatisfiable: return "Requested range not satisfiable";
				case HttpStatusCode.ExpectationFailed: return "Expectation Failed";
				case HttpStatusCode.InternalServerError: return "Internal Server Error";
				case HttpStatusCode.NotImplemented: return "Not Implemented";
				case HttpStatusCode.BadGateway: return "Bad Gateway";
				case HttpStatusCode.ServiceUnavailable: return "Service Unavailable";
				case HttpStatusCode.GatewayTimeOut: return "Gateway Time-out";
				case HttpStatusCode.HTTPVersionnotsupported: return "HTTP Version not supported";
				default:
					return code.ToString();
			}
		}
	}
}
