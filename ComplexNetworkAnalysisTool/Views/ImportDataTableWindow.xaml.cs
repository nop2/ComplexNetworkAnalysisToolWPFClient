using System;
using System.Collections.Generic;
using System.IO;
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
using ComplexNetworkAnalysisTool.ViewModels;
using Microsoft.Win32;

namespace ComplexNetworkAnalysisTool.Views
{
    /// <summary>
    /// ImportDataTableWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ImportDataTableWindow : Window
    {
        public ImportDataTableWindow()
        {
            InitializeComponent();
        }

        private void ButtonVertex_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "请选择数据文件",
                Filter = "表格文件(*.csv;*.xls;*.xlsx;*.txt)|*.csv;*.xls;*.xlsx;*.txt"
            };

            var showDialog = openFileDialog.ShowDialog();
            if (showDialog == true)
            {
                VertexPathTextBox.Text = openFileDialog.FileName;
                // openFileDialog.FileName;
            }
        }

        private void ButtonEdge_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "请选择数据文件",
                Filter = "表格文件(*.csv;*.xls;*.xlsx;)|*.csv;*.xls;*.xlsx;"
            };

            var showDialog = openFileDialog.ShowDialog();
            if (showDialog == true)
            {
                EdgePathTextBox.Text = openFileDialog.FileName;
                // openFileDialog.FileName;
            }
        }

        private void ButtonRelate_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "请选择数据文件",
                Filter = "表格文件(*.csv;*.xls;*.xlsx;)|*.csv;*.xls;*.xlsx;"
            };

            var showDialog = openFileDialog.ShowDialog();
            if (showDialog == true)
            {
                RelatePathTextBox.Text = openFileDialog.FileName;
                // openFileDialog.FileName;
            }
        }

        private void ButtonImport_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Grid.IsEnabled = false;
                if (EdgePathTextBox.Text == "" || !File.Exists(EdgePathTextBox.Text))
                {
                    Grid.IsEnabled = true;
                    MessageBox.Show("边数据为空或文件不存在");
                    return;
                }

                if (VertexPathTextBox.Text.Trim() != "" && !File.Exists(VertexPathTextBox.Text))
                {
                    Grid.IsEnabled = true;
                    MessageBox.Show("节点文件不存在");
                    return;
                }

                if ((bool)IsOverlayCheck.IsChecked && (VertexPathTextBox.Text.Trim()=="" || RelatePathTextBox.Text.Trim() == "" || !File.Exists(VertexPathTextBox.Text)))
                {
                    Grid.IsEnabled = true;
                    MessageBox.Show("节点表、关系路径表路径为空或相关文件不存在");
                    return;
                }

                

                

                var dpClient = new DpClient();

                var success = dpClient.ImportData(
                    EdgePathTextBox.Text.Replace('\\', '/'),
                    VertexPathTextBox.Text.Replace('\\', '/'),
                    RelatePathTextBox.Text.Replace('\\', '/'),
                    (bool)IsDirectedCheck.IsChecked,
                    (bool)IsOverlayCheck.IsChecked,
                    out var columns);

                if (success)
                {
                    MessageBox.Show("导入成功");

                    Task.Run(() =>
                    {
                        GraphVisViewModel._vertexSizeBindingList = new List<string>
                        {
                            "_默认_", "_度_", "_介数_", "_紧密中心性_"
                        };
                        GraphVisViewModel._vertexColorBindingList = new List<string>
                        {
                            "_默认_", "_随机_", "_度_", "_介数_", "_紧密中心性_"
                        };
                        GraphVisViewModel._vertexLabelBindingList = new List<string>
                        {
                            "_默认_"
                        };

                        if (columns != null)
                        {
                            var dataColumns = columns.Split('$');
                            GraphVisViewModel._vertexSizeBindingList.AddRange(dataColumns);
                            GraphVisViewModel._vertexColorBindingList.AddRange(dataColumns);
                            GraphVisViewModel._vertexLabelBindingList.AddRange(dataColumns);
                        }
                    });
                }
                else
                {
                    MessageBox.Show("导入失败");
                }

                dpClient.Close();

                Grid.IsEnabled = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "导入数据表");
            }
        }

        private void IsOverlayCheck_OnChecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("1.请确保节点id列存在\n\r2.关系网络第1列为其他网络节点id，第二列为当前网络节点id\n\r3.当需要三个及三个以上网络叠加时需要每个网络的id都不重复");
        }
    }
}