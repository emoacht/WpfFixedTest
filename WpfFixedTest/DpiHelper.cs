using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static WpfFixedTest.DisplayMonitorHelper;

namespace WpfFixedTest;

internal static class DpiHelper
{
	#region Win32

	[DllImport("Shcore.dll")]
	private static extern int GetDpiForMonitor(
		IntPtr hmonitor,
		MONITOR_DPI_TYPE dpiType,
		out uint dpiX,
		out uint dpiY);

	private enum MONITOR_DPI_TYPE
	{
		MDT_Effective_DPI = 0,
		MDT_Angular_DPI = 1,
		MDT_Raw_DPI = 2,
		MDT_Default = MDT_Effective_DPI
	}

	private const int S_OK = 0;

	#endregion

	public const double DefaultPixelsPerInch = 96D; // Default pixels per Inch

	public static DpiScale GetDpi(Window window)
	{
		var monitorHandle = GetMonitorHandle(window);
		if (monitorHandle != IntPtr.Zero)
		{
			if (GetDpiForMonitor(
				monitorHandle,
				MONITOR_DPI_TYPE.MDT_Default,
				out uint dpiX,
				out uint dpiY) is S_OK)
			{
				return new DpiScale(dpiX / DefaultPixelsPerInch,
									dpiY / DefaultPixelsPerInch);
			}
		}
		return new DpiScale(1, 1);
	}
}