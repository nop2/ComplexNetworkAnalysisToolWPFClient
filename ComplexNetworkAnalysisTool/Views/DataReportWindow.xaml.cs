using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ComplexNetworkAnalysisTool.Tools;

namespace ComplexNetworkAnalysisTool.Views
{
    /// <summary>
    /// DataReportWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataReportWindow : Window
    {
        public DataReportWindow(string type="DataReport")
        {
            InitializeComponent();

            if (type == "degree")
            {
                var reportPath = $"{Directory.GetCurrentDirectory()}\\Temp\\DegreeDistribution.html";
                ReportBrowser.Address = reportPath.Replace("#", "%23");
            }
            else
            {
                Task.Run(() =>
                {
                    var reportPath = $"{Directory.GetCurrentDirectory()}\\Temp\\DataReport.html";
                    var dpClient = new DpClient();
                    if (!dpClient.GetReport(reportPath))
                    {
                        dpClient.Close();
                        return;
                    }
                    dpClient.Close();
                    Dispatcher.Invoke(() =>
                    {
                        ReportBrowser.Address = reportPath.Replace("#", "%23");
                    });
                });
            }

            
        }


        private void DataReportWindow_OnClosed(object sender, EventArgs e)
        {
            // var dpClient = new DpClient();
            // dpClient.Cancel();
            // dpClient.Close();
        }
    }
}
