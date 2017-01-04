namespace GameLogWatcher
{
	interface ILogWatcherFactory
	{
		ILogWatcher CreateWatcher(string name, DynamicYaml config);
	}
}
