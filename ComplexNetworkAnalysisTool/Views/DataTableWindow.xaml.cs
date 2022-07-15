using System;
using System.Collections.Generic;
using System.Data;
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
using CefSharp;
using ComplexNetworkAnalysisTool.Tools;
using GalaSoft.MvvmLight;

namespace ComplexNetworkAnalysisTool.Views
{
    /// <summary>
    /// DataTableWindow.xaml 的交互逻辑
    /// </summary>
    ///
    public class Data : ViewModelBase
    {
        private DataTable _vertexDataTable = null;
        private DataTable _edgeDataTable = null;

        public DataTable VertexDataTable
        {
            get => _vertexDataTable;
            set
            {
                _vertexDataTable = value;
                RaisePropertyChanged();
            }
        }

        public DataTable EdgeDataTable
        {
            get => _edgeDataTable;
            set
            {
                _edgeDataTable = value;
                RaisePropertyChanged();
            }
        }

        public Data(DataTable vertexDataTable, DataTable edgeDataTable)
        {
            VertexDataTable = vertexDataTable;
            EdgeDataTable = edgeDataTable;
        }
    }

    public partial class DataTableWindow : Window
    {
        public static DataTable VertexDataTable { get; set; } = null;
        public static DataTable EdgeDataTable { get; set; } = null;

        private Data data;

        public DataTableWindow()
        {
            InitializeComponent();
            data = new Data(VertexDataTable, EdgeDataTable);
            this.DataContext = data;
        }

        public void RefreshData()
        {
            Task.Run(() =>
            {
                try
                {
                    Dispatcher.Invoke(() => { Grid.IsEnabled = false; });

                    var dpClient = new DpClient();
                    var csv = dpClient.PeekData(dataFrame: DataFrame.vertex_data, type: false);
                    if (csv == null)
                    {
                        Dispatcher.Invoke(() => { Grid.IsEnabled = true; });
                        return;
                    }


                    data.VertexDataTable = DataReader.GetDataTableFromStr(csv);
                    // foreach (DataColumn dataColumn in data.VertexDataTable.Columns)
                    // {
                    //     dataColumn.ColumnName = dataColumn.ColumnName.Replace("_","-");
                    // }
                    VertexDataTable = data.VertexDataTable;
                 

                    csv = dpClient.PeekData(dataFrame: DataFrame.edge_data, type: false);
                    if (csv == null)
                    {
                        Dispatcher.Invoke(() => { Grid.IsEnabled = true; });
                        return;
                    }

                    data.EdgeDataTable = DataReader.GetDataTableFromStr(csv);
                    // foreach (DataColumn dataColumn in data.EdgeDataTable.Columns)
                    // {
                    //     dataColumn.ColumnName = dataColumn.ColumnName.Replace("_", "-");
                    // }
                    EdgeDataTable = data.EdgeDataTable;
                    dpClient.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                finally
                {
                    Dispatcher.Invoke(() => { Grid.IsEnabled = true; });
                }
            });
        }

        private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void ButtonExportData_OnClick(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            // MessageBox.Show(b.Content.ToString());
            Grid.IsEnabled = false;

            try
            {
                string path;
                DataFrame df;
                if (b.Content.ToString() == "导出边表")
                {
                    path = $@"{Directory.GetCurrentDirectory()}\Temp\edge_data.csv";
                    df = DataFrame.edge_data;
                }
                else
                {
                    path = $@"{Directory.GetCurrentDirectory()}\Temp\vertex_data.csv";
                    df = DataFrame.vertex_data;
                }

                var dpClient = new DpClient();
                if (!dpClient.ExportCsv(path.Replace("\\", "/"), df))
                {
                    Grid.IsEnabled = true;
                    dpClient.Close();
                    MessageBox.Show("导出失败");
                    return;
                }

                path.OpenFileLocation();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
            finally
            {
                Grid.IsEnabled = true;
            }
        }
    }
}