using System;
using System.Collections.Generic;
using System.Data;
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
using ComplexNetworkAnalysisTool.Tools;
using GalaSoft.MvvmLight;

namespace ComplexNetworkAnalysisTool.Views
{
    /// <summary>
    /// ShortestPathWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShortestPathWindow : Window
    {
        private PathData pathData;
        public ShortestPathWindow(int startId,int endId,string label)
        {
            InitializeComponent();

            pathData = new PathData();
            this.DataContext = pathData;

            Task.Run(() =>
            {
                try
                {
                    var dpClient = new DpClient();
                    var csv = dpClient.GetShortestPath(startId, endId, label);
                    pathData.PathDataTable = DataReader.GetDataTableFromStr(csv);
                    dpClient.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "最短路径");
                }
            });
        }
    }

    public class PathData:ViewModelBase
    {
        private DataTable _pathDataTable;

        public DataTable PathDataTable
        {
            get => _pathDataTable;
            set { _pathDataTable = value; RaisePropertyChanged();}
        }
    }
}
