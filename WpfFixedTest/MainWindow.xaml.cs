using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Devices.Display;

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

	public DpiScale InitialDpi { get; private set; }

	protected override async void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);

		// VisualTreeHelper.GetDpi method seems to return System DPI even if the window doesn't
		// start in the primary monitor.
		InitialDpi = DpiHelper.GetDpi(this);
		Debug.WriteLine($"Initial DPI: {InitialDpi.PixelsPerDip}");

		await AdjustAsync();
	}

	private const int WM_DPICHANGED = 0x02E0;

	private IntPtr WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg is WM_DPICHANGED)
		{
			handled = true;
		}
		return IntPtr.Zero;
	}

	protected override void OnClosed(EventArgs e)
	{
		_source?.RemoveHook(WndProc);

		base.OnClosed(e);
	}

	private void WindowRoot_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (Mouse.LeftButton is MouseButtonState.Pressed)
		{
			this.DragMove();
		}
	}

	private async void Check_Click(object sender, RoutedEventArgs e)
	{
		await AdjustAsync();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		this.Close();
	}

	public DisplayMonitor? MonitorInfo
	{
		get { return (DisplayMonitor?)GetValue(MonitorInfoProperty); }
		set { SetValue(MonitorInfoProperty, value); }
	}
	public static readonly DependencyProperty MonitorInfoProperty =
		DependencyProperty.Register("MonitorInfo", typeof(DisplayMonitor), typeof(MainWindow), new PropertyMetadata(defaultValue: null));

	public double ExpectedPhysicalWidthInInches
	{
		get { return (double)GetValue(ExpectedPhysicalWidthInInchesProperty); }
		set { SetValue(ExpectedPhysicalWidthInInchesProperty, value); }
	}
	public static readonly DependencyProperty ExpectedPhysicalWidthInInchesProperty =
		DependencyProperty.Register("ExpectedPhysicalWidthInInches", typeof(double), typeof(MainWindow), new PropertyMetadata(0D));

	private async Task AdjustAsync()
	{
		MonitorInfo = await DisplayMonitorHelper.GetDisplayMonitorAsync(this);
		if (MonitorInfo is null)
			return;

		var (factorX, factorY) = (MonitorInfo.RawDpiX / InitialDpi.PixelsPerInchX,
								  MonitorInfo.RawDpiY / InitialDpi.PixelsPerInchY);

		var content = this.Content as FrameworkElement;
		if (content is not null)
		{
			content.LayoutTransform = new ScaleTransform(factorX, factorY);
			ExpectedPhysicalWidthInInches = content.Width / DpiHelper.DefaultPixelsPerInch;
		}
	}
}