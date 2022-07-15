using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ComplexNetworkAnalysisTool.ViewModels;
using System.Windows;
using System.Windows.Input;
using CefSharp;
using ComplexNetworkAnalysisTool.Tools;
using RDotNet;

namespace ComplexNetworkAnalysisTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel mainViewModel;
        public static REngine Engine;
        private Process serverProcess;


        public MainWindow()
        {
            InitializeComponent();

            //初始化ViewModel
            mainViewModel = new MainViewModel(CheckComboBoxAbnormalValues, CheckComboBoxStandardizedData,
                CheckComboBoxOneHot, CheckComboBoxSimilarityCalculation, Browser);
            DataContext = mainViewModel;

            //开启服务端，用于接收错误消息
            Task.Run(() =>
            {
                var sever = new TcpListener(IPAddress.Parse("127.0.0.1"), 33334);
                sever.Start(1);
                var buffer = new byte[1024 * 32];
                while (true)
                {
                    var client = sever.AcceptTcpClient();
                    var stream = client.GetStream();
                    stream.Read(buffer, 0, buffer.Length);
                    var msg = Encoding.UTF8.GetString(buffer);
                    MessageBox.Show(msg, "错误", MessageBoxButton.OK);
                    stream.Close();
                    client.Close();
                }
            });

            // return;
            try
            {
                var processes = Process.GetProcessesByName("Server.exe");
                if (processes == null || processes.Length == 0)
                {
                    serverProcess = Process.Start($@"{Directory.GetCurrentDirectory()}/Server/Server.exe");

                }

                // MessageBox.Show(serverProcess.Id.ToString());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "启动程序失败");
            }
        }

        /// <summary>
        /// 拖拽文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElement_OnDrop_(object sender, DragEventArgs e)
        {
            var fileName = ((System.Array)e.Data.GetData(DataFormats.FileDrop))?.GetValue(0).ToString();
            mainViewModel.FilePath = fileName;
        }

        /// <summary>
        /// 窗口关闭时要干的事情
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            serverProcess?.Kill();
        }

        private void UIElement_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var re = new Regex("[^0-9]+");
            e.Handled = re.IsMatch(e.Text);
        }
    }
}