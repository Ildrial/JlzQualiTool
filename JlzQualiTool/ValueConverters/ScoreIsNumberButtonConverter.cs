using System;
using System.Globalization;
using System.Windows.Data;

namespace JlzQualiTool.ValueConverters
{
    public class ScoreIsNumberButtonConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (string value in values)
            {
                if (!int.TryParse(value, out int intValue))
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}