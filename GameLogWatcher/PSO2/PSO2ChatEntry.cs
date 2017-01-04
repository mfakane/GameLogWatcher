using System;
using System.Text.RegularExpressions;
using Linearstar.Core.Slack;

namespace GameLogWatcher.PSO2
{
	class PSO2ChatEntry
	{
		public static readonly Regex CommandRegex = new Regex(@"^(/(ci[0-9](\s(?:nw|s[0-9]+|[0-9]+|t[0-9]+))*|(cmf|camouflage|cs|costume) .+?|[mfc]?la [a-zA-Z0-9_]+|ce(?: off| on)?|uioff(?: [0-9]+)?|[A-Za-z0-9]+)(\s|$))+", RegexOptions.Compiled);

		public DateTime DateTime
		{
			get;
		}

		public int Index
		{
			get;
		}

		public PSO2ChatChannel Channel
		{
			get;
		}

		public int SenderId
		{
			get;
		}

		public string Name
		{
			get;
		}

		public string Text
		{
			get;
		}

		public string TextWithoutCommand
		{
			get;
		}

		public bool HasCommand
		{
			get;
		}

		public PSO2ChatEntry(string line)
		{
			var sl = line.Split(new[] { '\t' }, 6);

			DateTime = DateTime.Parse(sl[0]);
			Index = int.Parse(sl[1]);
			Channel = (PSO2ChatChannel)Enum.Parse(typeof(PSO2ChatChannel), sl[2], true);
			SenderId = int.Parse(sl[3]);
			Name = sl[4];
			Text = sl[5].StartsWith("\"") && sl[5].EndsWith("\"") ? sl[5].Substring(1, sl[5].Length - 2) : sl[5];

			if (HasCommand = CommandRegex.IsMatch(Text))
				TextWithoutCommand = CommandRegex.Replace(Text, "");
			else
				TextWithoutCommand = Text;
		}

		public WebhookMessage ToMessage() =>
			new WebhookMessage(GetEmojiForChannel(Channel) + TextWithoutCommand)
			{
				UserName = Name,
			};

		static string GetEmojiForChannel(PSO2ChatChannel channel)
		{
			switch (channel)
			{
				case PSO2ChatChannel.Party:
					return ":pso2-party: ";
				case PSO2ChatChannel.Guild:
					return ":pso2-guild: ";
				case PSO2ChatChannel.Reply:
					return ":pso2-reply: ";
				default:
					return null;
			}
		}
	}
}
