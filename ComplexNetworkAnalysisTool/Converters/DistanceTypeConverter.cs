using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace ComplexNetworkAnalysisTool.Converters
{
    class DistanceTypeConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = (ComboBoxItem) value;
            if (item != null)
            {
                var match = Regex.Match(item.Content.ToString(), @"[a-z]+$");
                return match.Value;

                //return item.Content.ToString().Split(' ').Last().ToLower();
            }
            return "euclidean";
        }
    }
}
