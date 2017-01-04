using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameLogWatcher
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var notifyIcon = new NotifyIcon
			{
				Text = "GameLogWatcher",
				Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(GameLogWatcher)}.App.ico")),
				Visible = true,
				ContextMenu = new ContextMenu(new[]
				{
					new MenuItem("終了(&X)", (sender, e) => Application.Exit()),
				}),
			})
			{
				var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "config.yml");
				dynamic config = DynamicYaml.Parse(File.ReadAllText(path));
				var convention = new ConventionBuilder();

				convention.ForTypesDerivedFrom<ILogWatcherFactory>()
						  .Export<ILogWatcherFactory>();

				var host = new ContainerConfiguration()
					.WithDefaultConventions(convention)
					.WithAssembly(Assembly.GetExecutingAssembly())
					.CreateContainer();

				using (var cts = new CancellationTokenSource())
				{
					StartAsync((dynamic[])config.watchers, host.GetExports<ILogWatcherFactory>(), cts.Token);
					Application.Run();
					cts.Cancel();
					notifyIcon.Visible = false;
				}
			}
		}

		static Task StartAsync(dynamic[] watchers, IEnumerable<ILogWatcherFactory> factories, CancellationToken cancellationToken) =>
			Task.WhenAll(watchers
				.Where(i => i.enabled() ? i.enabled : true)
				.Select(i => factories.Aggregate(default(ILogWatcher), (x, y) => x ?? y.CreateWatcher((string)i.watch, i)))
				.Where(i => i != null)
				.Select(i => i.StartAsync(cancellationToken)));
	}
}
