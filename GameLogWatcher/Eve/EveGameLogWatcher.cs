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
			WhenForeground = config.foreground ?? false;
			WhenBackground = config.background ?? true;
			Kind = config.kind() ? EnumEx.ParseFlags<EveGameEntryKind>((string)config.kind, true) : EveGameEntryKind.All;
			Keywords = config.keywords;
			Where = config.where() ? DynamicExpression.ParseLambda<EveGameEntry, bool>((string)config.where).Compile() : _ => true;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var date = DateTime.UtcNow.Date;
				var logFiles = GetLogFiles(date).ToDictionary(i => i, i => WatchAsync(date, i, true, cancellationToken));

				using (var fsw = new FileSystemWatcher(LogDirectory, GetLogPattern(date)))
					while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow.Date == date)
					{
						var change = fsw.WaitForChanged(WatcherChangeTypes.Created, 100);

						if (!change.TimedOut && !cancellationToken.IsCancellationRequested)
							logFiles[change.Name] = WatchAsync(date, change.Name, false, cancellationToken);
					}

				await Task.WhenAll(logFiles.Values);
				await Task.Delay(100, cancellationToken);
			}
		}

		async Task WatchAsync(DateTime startDate, string path, bool readNewEntryOnly, CancellationToken cancellationToken)
		{
			using (var reader = new LogReader(path, Encoding.UTF8, readNewEntryOnly))
				while (!cancellationToken.IsCancellationRequested
					&& DateTime.UtcNow.Date == startDate)
				{
					if (!(await reader.ReadEntryAsync(cancellationToken) is string entry) ||
						!(EveGameEntry.Parse(entry) is EveGameEntry gameEntry) ||
						!IsMatch(gameEntry))
						continue;

					await IncomingWebhooks.PostAsync(WebhookEndPoint, gameEntry.ToMessage());
				}
		}

		bool IsMatch(EveGameEntry entry) =>
			(WhenForeground && EveValues.IsWindowActive || WhenBackground && !EveValues.IsWindowActive) &&
			(entry.Kind & Kind) != 0 &&
			(Keywords?.Any(i => entry.Content.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1) ?? true) &&
			Where(entry);

		static string GetLogPattern(DateTime date) =>
			$"{date.ToUniversalTime():yyyyMMdd}_*.txt";

		static IEnumerable<string> GetLogFiles(DateTime date) =>
			Directory.EnumerateFiles(LogDirectory, GetLogPattern(date));
	}
}
