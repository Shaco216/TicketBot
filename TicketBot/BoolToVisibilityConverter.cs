using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TicketBot;

public class BoolToVisibilityConverter : IValueConverter
{
    // Konvertiert bool -> Visibility (Visible / Collapsed).
    // Optionaler Converter-Parameter "Invert" kehrt das Ergebnis um.
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = value is bool b && b;

        if (parameter is string p && p.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    // Konvertiert Visibility -> bool (Visible => true). Unterstützt ebenfalls "Invert".
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            bool result = v == Visibility.Visible;

            if (parameter is string p && p.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                result = !result;
            }

            return result;
        }

        return false;
    }
}
