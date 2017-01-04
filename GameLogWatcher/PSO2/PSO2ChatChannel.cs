using System;

namespace GameLogWatcher.PSO2
{
	[Flags]
	enum PSO2ChatChannel
	{
		None,
		Public = 0b1,
		Party = 0b10,
		Guild = 0b100,
		Reply = 0b1000,
		All = Public | Party | Guild | Reply,
	}
}
