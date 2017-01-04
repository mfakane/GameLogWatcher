using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace GameLogWatcher
{
	static class NativeMethods
	{
		[SuppressUnmanagedCodeSecurity, DllImport("user32")]
		public static extern IntPtr GetForegroundWindow();
		[SuppressUnmanagedCodeSecurity, DllImport("user32")]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		public static string GetClassName(this IntPtr hWnd)
		{
			var sb = new StringBuilder(256);

			GetClassName(hWnd, sb, sb.Capacity);

			return sb.ToString();
		}

		[SuppressUnmanagedCodeSecurity, DllImport("user32")]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
		[SuppressUnmanagedCodeSecurity, DllImport("user32")]
		public static extern int GetWindowTextLength(IntPtr hWnd);

		public static string GetWindowText(this IntPtr hWnd)
		{
			var sb = new StringBuilder(GetWindowTextLength(hWnd) + 1);

			GetWindowText(hWnd, sb, sb.Capacity);

			return sb.ToString();
		}
	}
}
