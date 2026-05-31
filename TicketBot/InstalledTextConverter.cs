using System.Globalization;
using System.Windows.Data;

namespace TicketBot;

public class InstalledTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? "(installiert)" : "(nicht installiert)";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
