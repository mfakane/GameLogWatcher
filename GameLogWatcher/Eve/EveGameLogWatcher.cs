using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Linearstar.Core.Slack;

namespace GameLogWatcher.Eve
{
	class EveGameLogWatcher : ILogWatcher
	{
		static string LogDirectory = Path.Combine(EveValues.DefaultDataDirectory, "logs", "Gamelogs");

		public static bool CanCreateInstance(dynamic config) =>
			Directory.Exists(LogDirectory);

		public string WebhookEndPoint { get; }
		public bool WhenForeground { get; }
		public bool WhenBackground { get; }
		public EveGameEntryKind Kind { get; }
		public string[] Keywords { get; }
		public Func<EveGameEntry, bool> Where { get; }

		public EveGameLogWatcher(dynamic config)
		{
			WebhookEndPoint = config.webhook;
			WhenForeground = config.foreground() ? config.foreground : false;
			WhenBackground = config.background() ? config.background : true;
			Kind = config.kind() ? EnumEx.ParseFlags<EveGameEntryKind>((string)config.kind, true) : EveGameEntryKind.All;
			Keywords = config.keywords() ? config.keywords : null;
			Where = config.where() ? DynamicExpression.ParseLambda<EveGameEntry, bool>((string)config.where).Compile() : _ => true;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var startDate = DateTime.UtcNow.Date;
				var logFiles = GetLogFiles(startDate);

				if (logFiles.Any())
					await Task.WhenAny(logFiles.Select(async path =>
					{
						using (var reader = new LogReader(path, Encoding.UTF8))
							while (!cancellationToken.IsCancellationRequested
								&& DateTime.UtcNow.Date == startDate)
							{
								if (!(await reader.ReadEntryAsync(cancellationToken) is string entry) ||
									!(EveGameEntry.Parse(entry) is EveGameEntry gameEntry) ||
									!IsMatch(gameEntry))
									continue;

								await IncomingWebhooks.PostAsync(WebhookEndPoint, gameEntry.ToMessage());
							}
					}));
				else
					await Task.Delay(1000, cancellationToken);
			}
		}

		bool IsMatch(EveGameEntry entry) =>
			(WhenForeground && EveValues.IsWindowActive || WhenBackground && !EveValues.IsWindowActive) &&
			(entry.Kind & Kind) != 0 &&
			(Keywords?.Any(i => entry.Content.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1) ?? true) &&
			Where(entry);

		static IEnumerable<string> GetLogFiles(DateTime date) =>
			Directory.EnumerateFiles(LogDirectory, $"{date.ToUniversalTime():yyyyMMdd}_*.txt");
	}
}
