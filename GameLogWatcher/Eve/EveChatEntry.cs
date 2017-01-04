using System;
using Linearstar.Core.Slack;

namespace GameLogWatcher.Eve
{
	class EveChatEntry
	{
		public DateTimeOffset DateTime { get; }
		public string Channel { get; }
		public string Name { get; }
		public string Content { get; }

		EveChatEntry(DateTimeOffset dateTime, string channel, string name, string content)
		{
			DateTime = dateTime;
			Channel = channel;
			Name = name;
			Content = content;
		}

		public static EveChatEntry Parse(string channel, string line)
		{
			if (!line.StartsWith("[")) return null;

			var idx = line.IndexOf(']');
			var idx2 = line.IndexOf('>', idx);

			return new EveChatEntry
			(
				DateTimeOffset.Parse(line.Substring(0, idx).Trim('[', ']', ' ')),
				channel,
				line.Substring(idx + 1, idx2 - idx - 1).Trim(),
				line.Substring(idx2 + 2)
			);
		}

		public WebhookMessage ToMessage() =>
			new WebhookMessage
			{
				UserName = Name,
				Text = Content.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"),
			};
	}
}
