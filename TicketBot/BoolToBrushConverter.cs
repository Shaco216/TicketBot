using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TicketBot;

public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? Brushes.LimeGreen : Brushes.LightGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
