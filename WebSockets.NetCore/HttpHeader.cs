using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.NetCore
{
	public abstract class HttpHeader
	{
		public string Version { get; set; } = "HTTP/1.1";
		public Dictionary<string, string> Fields { get; private set; } = new Dictionary<string, string>();

		public string this[string fieldName]
		{
			get
			{
				string value = null;
				Fields.TryGetValue(fieldName, out value);
				return value;
			}
			set
			{
				if (Fields.ContainsKey(fieldName))
				{
					Fields[fieldName] = value;
				}
				else
				{
					Fields.Add(fieldName, value);
				}
			}
		}

		protected virtual void Parse(string[] lines)
		{
			if (lines.Length <= 1)
			{
				return;
			}

			foreach (var field in lines.Skip(1))
			{
				Fields.Add(GetFieldName(field), GetFieldValue(field));
			}
		}

		public void Read(Stream stream)
		{
			Parse(ReadHeaderLines(stream));
		}

		public void Write(Stream stream)
		{
			WriteHeaderLines(stream, this);
		}

		protected static string[] ReadHeaderLines(Stream stream)
		{
			StreamReader reader = new StreamReader(stream, Encoding.UTF8);

			List<string> lines = new List<string>();
			string line;

			while (((line = reader.ReadLine())?.Length ?? 0) > 0)
			{
				lines.Add(line);
			}

			return lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
		}

		protected static void WriteHeaderLines(Stream stream, HttpHeader header)
		{
			StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

			writer.WriteLine(header.ToString());
			writer.WriteLine();
			writer.Flush();
		}

		private static string GetFieldName(string line)
		{
			int index = line.IndexOf(':');

			if (index == -1)
			{
				return line;
			}

			return line.Substring(0, index).Trim();
		}

		private static string GetFieldValue(string line)
		{
			int index = line.IndexOf(':');

			if (index == -1)
			{
				return "";
			}

			return line.Substring(index + 1).Trim();
		}

		public override string ToString()
		{
			return string.Join("\r\n", Fields.Select(kv => kv.Key + ": " + kv.Value));
		}
	}
}
