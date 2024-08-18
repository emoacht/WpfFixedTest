using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;

namespace WpfFixedTest;

internal static class DisplayMonitorHelper
{
	#region Win32

	[DllImport("User32.dll")]
	private static extern IntPtr MonitorFromWindow(
		IntPtr hwnd,
		MONITOR_DEFAULTTO dwFlags);

	private enum MONITOR_DEFAULTTO : uint
	{
		MONITOR_DEFAULTTONULL = 0x00000000,
		MONITOR_DEFAULTTOPRIMARY = 0x00000001,
		MONITOR_DEFAULTTONEAREST = 0x00000002,
	}

	[DllImport("User32.dll", CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumDisplayDevices(
		string? lpDevice,
		uint iDevNum,
		ref DISPLAY_DEVICE lpDisplayDevice,
		uint dwFlags);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	private struct DISPLAY_DEVICE
	{
		public uint cb;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string DeviceName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceString;

		public uint StateFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceID;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceKey;
	}

	private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

	[DllImport("User32.dll", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetMonitorInfo(
		IntPtr hMonitor,
		ref MONITORINFOEX lpmi);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct MONITORINFOEX
	{
		public uint cbSize;
		public RECT rcMonitor;
		public RECT rcWork;
		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string szDevice;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	#endregion

	public static async Task<DisplayMonitor?> GetDisplayMonitorAsync(Window window)
	{
		var monitorHandle = GetMonitorHandle(window);
		if (monitorHandle != IntPtr.Zero)
		{
			if (TryGetMonitorDeviceId(monitorHandle, out var deviceId))
			{
				var devices = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector());
				if (devices is { Count: > 0 })
				{
					foreach (var device in devices.Where(x => x.Id == deviceId))
					{
						return await DisplayMonitor.FromInterfaceIdAsync(device.Id);
					}
				}
			}
		}
		return null;
	}

	internal static IntPtr GetMonitorHandle(Window window)
	{
		var windowHandle = new WindowInteropHelper(window).EnsureHandle();

		return MonitorFromWindow(
			windowHandle,
			MONITOR_DEFAULTTO.MONITOR_DEFAULTTONULL);
	}

	private static bool TryGetMonitorDeviceId(nint monitorHandle, out string monitorDeviceId)
	{
		var monitorDeviceName = GetMonitorDeviceName(monitorHandle);
		if (monitorDeviceName is not null)
		{
			foreach (var device in EnumerateDisplayDevices().Where(x => x.displayDeviceName == monitorDeviceName))
			{
				monitorDeviceId = device.monitorDeviceId;
				return true;
			}
		}
		monitorDeviceId = string.Empty;
		return false;
	}

	private static string? GetMonitorDeviceName(IntPtr monitorHandle)
	{
		var monitorInfo = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };

		if (GetMonitorInfo(monitorHandle, ref monitorInfo))
		{
			return monitorInfo.szDevice;
		}
		return null;
	}

	private static IEnumerable<(string displayDeviceName, string monitorDeviceId)> EnumerateDisplayDevices()
	{
		var size = (uint)Marshal.SizeOf<DISPLAY_DEVICE>();
		var display = new DISPLAY_DEVICE { cb = size };
		var monitor = new DISPLAY_DEVICE { cb = size };

		for (uint i = 0; EnumDisplayDevices(null, i, ref display, EDD_GET_DEVICE_INTERFACE_NAME); i++)
		{
			for (uint j = 0; EnumDisplayDevices(display.DeviceName, j, ref monitor, EDD_GET_DEVICE_INTERFACE_NAME); j++)
			{
				yield return (display.DeviceName, monitor.DeviceID);
			}
		}
	}
}