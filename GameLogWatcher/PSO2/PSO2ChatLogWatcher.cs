using System;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Linearstar.Core.Slack;

namespace GameLogWatcher.PSO2
{
	class PSO2ChatLogWatcher : ILogWatcher
	{
		static string LogDirectory = Path.Combine(PSO2Values.DefaultDataDirectory, "log");

		public static bool CanCreateInstance(dynamic config) =>
				Directory.Exists(LogDirectory);

		public string WebhookEndPoint { get; }
		public bool WhenForeground { get; }
		public bool WhenBackground { get; }
		public PSO2ChatChannel Channel { get; }
		public string[] Keywords { get; }
		public Func<PSO2ChatEntry, bool> Where { get; }

		public PSO2ChatLogWatcher(dynamic config)
		{
			WebhookEndPoint = config.webhook;
			WhenForeground = config.foreground() ? config.foreground : false;
			WhenBackground = config.background() ? config.background : true;
			Channel = EnumEx.ParseFlags<PSO2ChatChannel>((string)config.channel, true);
			Keywords = config.keywords() ? config.keywords : null;
			Where = config.where() ? DynamicExpression.ParseLambda<PSO2ChatEntry, bool>((string)config.where).Compile() : _ => true;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var startDate = DateTime.UtcNow.Date;

				using (var reader = new LogReader(Path.Combine(LogDirectory, $"ChatLog{startDate.ToUniversalTime():yyyyMMdd}_00.txt"), Encoding.Unicode))
					while (!cancellationToken.IsCancellationRequested
						&& DateTime.UtcNow.Date == startDate)
					{
						if (!(await reader.ReadEntryAsync(cancellationToken) is string entry)) continue;

						var chatEntry = new PSO2ChatEntry(entry);

						if (!IsMatch(chatEntry)) continue;

						await IncomingWebhooks.PostAsync(WebhookEndPoint, chatEntry.ToMessage());
					}
			}
		}

		bool IsMatch(PSO2ChatEntry entry) =>
			(WhenForeground && PSO2Values.IsWindowActive || WhenBackground && !PSO2Values.IsWindowActive) &&
			(entry.Channel & Channel) != 0 &&
			(Keywords?.Any(i => entry.TextWithoutCommand.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1) ?? true) &&
			Where(entry);

	}
}
