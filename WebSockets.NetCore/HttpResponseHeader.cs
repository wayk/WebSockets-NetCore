namespace WebSockets.NetCore
{
	public class HttpResponseHeader : HttpHeader
	{
		private HttpStatusCode code;

		public HttpStatusCode Code
		{
			get { return code; }
			set
			{
				code = value;
				Reason = code.GetReason();
			}
		}

		public string Reason { get; set; }

		protected override void Parse(string[] lines)
		{
			base.Parse(lines);

			string[] splitted = lines[0].Split(' ');

			Version = splitted[0];
			Code = (HttpStatusCode)int.Parse(splitted[1]);
			Reason = splitted[2];
		}

		public override string ToString()
		{
			return $"{Version} {(int)Code} {Reason}\r\n" + base.ToString();
		}
	}
}
