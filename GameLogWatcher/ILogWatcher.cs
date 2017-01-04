using System.Threading;
using System.Threading.Tasks;

namespace GameLogWatcher
{
	interface ILogWatcher
	{
		Task StartAsync(CancellationToken cancellationToken);
	}
}