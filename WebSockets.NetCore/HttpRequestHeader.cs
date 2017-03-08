namespace WebSockets.NetCore
{
	public class HttpRequestHeader : HttpHeader
	{
		public string Method { get; set; }
		public string Path { get; set; }

		protected override void Parse(string[] lines)
		{
			if (lines.Length <= 1)
			{
				return;
			}

			base.Parse(lines);

			string[] splitted = lines[0].Split(' ');

			Method = splitted[0];
			Path = splitted[1];
			Version = splitted[2];
		}

		public override string ToString()
		{
			return $"{Method} {Path} {Version}\r\n" + base.ToString();
		}
	}
}
