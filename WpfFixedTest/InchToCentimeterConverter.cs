using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfFixedTest;

[ValueConversion(typeof(double), typeof(double))]
internal class InchToCentimeterConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((value is double v) || double.TryParse(value?.ToString(), out v))
			return v * 2.54;

		return DependencyProperty.UnsetValue;		
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((value is double v) || double.TryParse(value?.ToString(), out v))
			return v / 2.54;

		return DependencyProperty.UnsetValue;
	}
}