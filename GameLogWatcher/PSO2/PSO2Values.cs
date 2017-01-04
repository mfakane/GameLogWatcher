using System;
using System.IO;

namespace GameLogWatcher.PSO2
{
	static class PSO2Values
	{
		public static readonly string DefaultDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SEGA", "PHANTASYSTARONLINE2");
		public const string WindowTitle = "Phantasy Star Online 2";
		public const string WindowClassName = "Phantasy Star Online 2";

		public static bool IsWindowActive
		{
			get
			{
				var hWnd = NativeMethods.GetForegroundWindow();

				return hWnd.GetClassName() == WindowClassName
					&& hWnd.GetWindowText() == WindowTitle;
			}
		}
	}
}
