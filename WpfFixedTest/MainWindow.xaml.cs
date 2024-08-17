using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WpfFixedTest;

public partial class MainWindow : Window
{
	private readonly HwndSource _source;

	public MainWindow()
	{
		InitializeComponent();

		var handle = new WindowInteropHelper(this).EnsureHandle();
		_source = HwndSource.FromHwnd(handle);
		_source.AddHook(WndProc);
	}

	public DpiScale FixedDpi { get; } = new DpiScale(1, 1);

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);

		var initialDpi = VisualTreeHelper.GetDpi(this);
		Debug.WriteLine($"Initial DPI: {initialDpi.PixelsPerDip}");
		var (factorX, factorY) = (FixedDpi.DpiScaleX / initialDpi.DpiScaleX, FixedDpi.DpiScaleY / initialDpi.DpiScaleY);

		var content = this.Content as FrameworkElement;
		if (content is not null)
		{
			content.LayoutTransform = new ScaleTransform(factorX, factorY);
		}
	}

	private const int WM_DPICHANGED = 0x02E0;

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg is WM_DPICHANGED)
		{
			Debug.WriteLine("DPI Changed");
			handled = true;
		}
		return IntPtr.Zero;
	}

	protected override void OnClosed(EventArgs e)
	{
		_source?.RemoveHook(WndProc);

		base.OnClosed(e);
	}
}