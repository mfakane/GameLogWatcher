using System;
using System.IO;

namespace GameLogWatcher.Eve
{
	static class EveValues
	{
		public static readonly string DefaultDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE");
		public const string WindowTitle = "EVE";
		public const string WindowClassName = "triuiScreen";

		public static bool IsWindowActive
		{
			get
			{
				var hWnd = NativeMethods.GetForegroundWindow();

				return hWnd.GetClassName() == WindowClassName
					&& hWnd.GetWindowText().StartsWith(WindowTitle);
			}
		}
	}
}
