using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Linearstar.Core.Slack;

namespace GameLogWatcher.Eve
{
	class EveChatLogWatcher : ILogWatcher
	{
		readonly ConcurrentDictionary<string, string> avatarUrls = new ConcurrentDictionary<string, string>();

		static string LogDirectory = Path.Combine(EveValues.DefaultDataDirectory, "logs", "Chatlogs");

		public string WebhookEndPoint { get; }
		public bool WhenForeground { get; }
		public bool WhenBackground { get; }
		public Regex Channel { get; }
		public string[] Keywords { get; }
		public Func<EveChatEntry, bool> Where { get; }

		public static bool CanCreateInstance(dynamic config) =>
			Directory.Exists(LogDirectory);

		public EveChatLogWatcher(dynamic config)
		{
			WebhookEndPoint = config.webhook;
			WhenForeground = config.foreground() ? config.foreground : false;
			WhenBackground = config.background() ? config.background : true;
			Channel = config.channel() ? new Regex((string)config.channel, RegexOptions.Compiled) : null;
			Keywords = config.keywords() ? config.keywords : null;
			Where = config.where() ? DynamicExpression.ParseLambda<EveChatEntry, bool>((string)config.where).Compile() : _ => true;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var startDate = DateTime.UtcNow.Date;
				var logFiles = GetLogFiles(startDate).ToDictionary(i => i, i => WatchAsync(startDate, i, true, cancellationToken));

				while (!cancellationToken.IsCancellationRequested
					&& DateTime.UtcNow.Date == startDate)
				{
					var newLogFiles = GetLogFiles(startDate).Except(logFiles.Keys);

					foreach (var i in newLogFiles)
						logFiles[i] = WatchAsync(startDate, i, false, cancellationToken);

					await Task.Delay(100, cancellationToken);
				}
			}
		}

		async Task WatchAsync(DateTime startDate, string path, bool readNewEntryOnly, CancellationToken cancellationToken)
		{
			var channel = GetChannelFromPath(path);

			using (var reader = new LogReader(path, Encoding.UTF8, readNewEntryOnly))
				while (!cancellationToken.IsCancellationRequested
					&& DateTime.UtcNow.Date == startDate)
				{
					if (!(await reader.ReadEntryAsync(cancellationToken) is string entry) ||
						!(EveChatEntry.Parse(channel, entry) is EveChatEntry chatEntry) ||
						!IsMatch(chatEntry))
						continue;

					var message = chatEntry.ToMessage();

					if (chatEntry.Name != "EVE System")
						message.IconUrl = GetAvatarUrl(chatEntry.Name);

					await IncomingWebhooks.PostAsync(WebhookEndPoint, message);
				}
		}

		bool IsMatch(EveChatEntry entry) =>
			(WhenForeground && EveValues.IsWindowActive || WhenBackground && !EveValues.IsWindowActive) &&
			(Keywords?.Any(i => entry.Content.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1) ?? true) &&
			Where(entry);

		string GetAvatarUrl(string name) =>
			avatarUrls.GetOrAdd(name, _ =>
			{
				var rt = EsiClient.SearchAsync(name, new[] { "character" }).Result.Characters;

				return rt.Any() ? $"https://image.eveonline.com/Character/{rt.First()}_256.jpg" : null;
			});

		IEnumerable<string> GetLogFiles(DateTime date) =>
			Directory.EnumerateFiles(LogDirectory, $"*_{date.ToUniversalTime():yyyyMMdd}_*.txt")
					 .Where(i => Channel?.IsMatch(GetChannelFromPath(i)) ?? true);

		static string GetChannelFromPath(string path) =>
			Path.GetFileName(path).Split('_')[0];
	}
}
