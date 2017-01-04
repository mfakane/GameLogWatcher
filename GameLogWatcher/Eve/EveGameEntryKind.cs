using System;

namespace GameLogWatcher.Eve
{
	[Flags]
	enum EveGameEntryKind
	{
		Unknown,
		None = 0b1,
		Notify = 0b10,
		Info = 0b100,
		Question = 0b1000,
		Warning = 0b10000,
		Combat =  0b100000,
		All = None | Notify | Info | Question | Warning | Combat,
	}
}
