using System;
using System.Text.RegularExpressions;
using Linearstar.Core.Slack;

namespace GameLogWatcher.Eve
{
	class EveGameEntry
	{
		static readonly Regex tag = new Regex("<(.+?)>", RegexOptions.Compiled);

		public DateTimeOffset DateTime { get; }
		public EveGameEntryKind Kind { get; }
		public string Content { get; }

		EveGameEntry(DateTimeOffset dateTime, string kind, string content)
		{
			DateTime = dateTime;
			Kind = Enum.TryParse<EveGameEntryKind>(kind, true, out var result) ? result : EveGameEntryKind.Unknown;
			Content = content;
		}

		public static EveGameEntry Parse(string line)
		{
			if (!line.StartsWith("[")) return null;

			var idx = line.IndexOf('(');
			var idx2 = line.IndexOf(')', idx);

			return new EveGameEntry
			(
				DateTimeOffset.Parse(line.Substring(0, idx).Trim('[', ']', ' ')),
				line.Substring(idx + 1, idx2 - idx - 1),
				tag.Replace(line.Substring(idx2 + 2), m =>
				{
					var tagName = m.Groups[1].Value;

					switch (tagName)
					{
						case "b":
						case "/b":
							return "*";
						case "br":
							return "\n";
						default:
							return null;
					}
				})
			);
		}

		public WebhookMessage ToMessage() =>
			new WebhookMessage
			{
				Attachments = new[]
				{
					new WebhookAttachment(Kind == EveGameEntryKind.None || Kind == EveGameEntryKind.Unknown ? null : $"({Kind}) " + Content)
					{
						Color = GetColor(),
						Text = Content,
						MrkdwnIn = WebhookAttachmentMrkdwnTargets.Text,
					},
				},
			};
		
		string GetColor()
		{
			switch (Kind)
			{
				case EveGameEntryKind.Notify:
					return "good";
				case EveGameEntryKind.Question:
				case EveGameEntryKind.Warning:
					return "warning";
				case EveGameEntryKind.Combat:
					if (Content.EndsWith(" misses you completely"))
						return null;
					else if (Content.Contains(" to "))
						return "#439fe0";
					else
						return "danger";
				default:
					return null;
			}
		}
	}
}
