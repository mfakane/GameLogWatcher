namespace GameLogWatcher.PSO2
{
	class PSO2LogWatcherFactory : ILogWatcherFactory
	{
		public ILogWatcher CreateWatcher(string name, DynamicYaml config)
		{
			switch (name)
			{
				case "pso2-chatlog":
					return PSO2ChatLogWatcher.CanCreateInstance(config) ? new PSO2ChatLogWatcher(config) : null;
				default:
					return null;
			}
		}
	}
}
