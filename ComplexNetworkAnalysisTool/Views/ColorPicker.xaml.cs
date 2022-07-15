using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ComplexNetworkAnalysisTool.ViewModels;

namespace ComplexNetworkAnalysisTool.Views
{
    /// <summary>
    /// ColorPicker.xaml 的交互逻辑
    /// </summary>
    public partial class ColorPicker : Window
    {
        public string Color { get; set; }

        public ColorPicker()
        {
            InitializeComponent();
            hcColorPicker.Confirmed += (sender, e) =>
            {
                Color = e.Info.ToString();
                confirm?.Invoke(this, EventArgs.Empty);

                Close();
            };
            hcColorPicker.Canceled += ((sender, args) => { Close(); });
        }

        public event EventHandler confirm;
    }
}