using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace ComplexNetworkAnalysisTool.Converters
{
    public class MaxDataRowsConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        /// <summary>
        /// 界面属性转向源，得到要预览的最大数据行数
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var comboBoxItem = value as ComboBoxItem;
            switch (comboBoxItem.Content.ToString())
            {
                case "预览100行数据": return 100;
                case "预览1000行数据": return 1000;
                case "预览10000行数据": return 10000;
                default: return int.MaxValue;
            }
        }
    }
}
