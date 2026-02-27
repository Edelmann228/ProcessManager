using System;
using System.Globalization;
using System.Windows.Data;

namespace ProcessManager.Converters
{
    public class IndexToCoreNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && int.TryParse(parameter.ToString(), out int index))
            {
                return $"Core {index + 1}";
            }

            
            if (value is bool)
            {
                return "Core";
            }

            return "Core";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}