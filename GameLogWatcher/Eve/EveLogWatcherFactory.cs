namespace GameLogWatcher.Eve
{
	class EveLogWatcherFactory : ILogWatcherFactory
	{
		public ILogWatcher CreateWatcher(string name, DynamicYaml config)
		{
			switch (name)
			{
				case "eve-chatlog":
					return EveChatLogWatcher.CanCreateInstance(config) ? new EveChatLogWatcher(config) : null;
				case "eve-gamelog":
					return EveGameLogWatcher.CanCreateInstance(config) ? new EveGameLogWatcher(config) : null;
				default:
					return null;
			}
		}
	}
}
