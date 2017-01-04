using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameLogWatcher
{
	public class LogReader : IDisposable
	{
		StreamReader streamReader;

		public StreamReader StreamReader =>
			streamReader ?? (streamReader = File.Exists(FileName) ? new StreamReader(new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding) : null);

		public Encoding Encoding { get; }
		public string FileName { get; }
		public string Delimiter { get; set; } = "\r\n";

		public LogReader(string fileName, Encoding encoding)
			: this(fileName, encoding, true)
		{
		}

		public LogReader(string fileName, Encoding encoding, bool readNewEntryOnly)
		{
			FileName = fileName;
			Encoding = encoding;

			if (readNewEntryOnly)
				StreamReader?.ReadToEnd();
		}

		public IList<string> ReadAllEntries() =>
			StreamReader.ReadToEnd()
				.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries)
				.ToArray();

		async Task<string> ReadEntryCoreAsync()
		{
			if (StreamReader == null ||
				StreamReader.BaseStream.Position == StreamReader.BaseStream.Length)
				return null;

			var sb = new StringBuilder();
			var chars = new StringBuilder();
			var buf = new char[1];
			var bom = StreamReader.CurrentEncoding.GetString(StreamReader.CurrentEncoding.GetPreamble());

			while (true)
			{
				var charsRead = await StreamReader.ReadAsync(buf, 0, buf.Length);

				if (charsRead == 0) break;

				var c = buf[0];

				sb.Append(c);
				chars.Append(c);

				if (!Delimiter.StartsWith(chars.ToString()))
					chars.Clear();
				else if (chars.ToString() == Delimiter)
				{
					sb.Remove(sb.Length - chars.Length, chars.Length);

					break;
				}
			}

			if (bom.Length > 0 && sb.Length > 0 && sb.ToString().Take(bom.Length).SequenceEqual(bom))
				sb.Remove(0, bom.Length);

			if (sb.Length > 0)
				return sb.ToString();
			else
				return null;
		}

		public async Task<string> ReadEntryAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var entry = await ReadEntryCoreAsync();

				if (entry != null)
					return entry;

				await Task.Delay(100, cancellationToken);
			}

			return null;
		}

		public void Dispose() =>
			StreamReader.Dispose();
	}
}
