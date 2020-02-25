using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace JlzQualiTool.ValueConverters
{
    public class DefaultButtonConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Contains(true);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}