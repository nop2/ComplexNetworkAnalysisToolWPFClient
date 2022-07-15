using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ComplexNetworkAnalysisTool.Tools;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HandyControl.Controls;

namespace ComplexNetworkAnalysisTool.ViewModels
{
    public class DataPreProcessingModel : ViewModelBase
    {
        private bool _dropDuplicates = false;
        private int _missingValueProcessing = 0;
        private bool _processAbnormalValue = false;
        private int _standardizeData = 0;
        private bool _ontHotEncoding = false;
        private bool _isOnProcessing = false;
        private static string savePath = @"Temp\processed_data.csv";
        public static DataTable processedDataTable = null;
        private Process _fileProcessingProcess = null;
        private bool _saveFileButtonEnabled = true;

        #region Binding Properties

        /// <summary>
        /// 是否删除重复行
        /// </summary>
        public bool IsDropDuplicates
        {
            get => _dropDuplicates;
            set
            {
                _dropDuplicates = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 缺失值处理
        /// </summary>
        public int MissingValueProcessing
        {
            get => _missingValueProcessing;
            set
            {
                _missingValueProcessing = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 是否检测处理异常值
        /// </summary>
        public bool IsProcessAbnormalValue
        {
            get => _processAbnormalValue;
            set
            {
                _processAbnormalValue = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 标准化数据方式
        /// </summary>
        public int StandardizeData
        {
            get => _standardizeData;
            set
            {
                _standardizeData = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 是否使用独热编码
        /// </summary>
        public bool IsOntHotEncoding
        {
            get => _ontHotEncoding;
            set
            {
                _ontHotEncoding = value;
                RaisePropertyChanged();
            }
        }

        public bool IsOnProcessing
        {
            get => _isOnProcessing;
            set
            {
                _isOnProcessing = value;
                RaisePropertyChanged();
            }
        }

        public DataTable ProcessedDataTable
        {
            get => processedDataTable;
            set
            {
                processedDataTable = value;
                RaisePropertyChanged();
            }
        }

        public bool SaveFileButtonEnabled
        {
            get => _saveFileButtonEnabled;
            set
            {
                _saveFileButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region UI Controls

        /// <summary>
        /// 异常值处理列选择框
        /// </summary>
        public CheckComboBox CheckComboBoxAbnormalValues { get; set; }

        public CheckComboBox CheckComboBoxStandardizedData { get; set; }
        public CheckComboBox CheckComboBoxOneHot { get; set; }

        #endregion


        #region Commands

        /// <summary>
        /// 预处理文件
        /// </summary>
        public RelayCommand ButtonProcessCommand { get; set; }

        /// <summary>
        /// 取消处理
        /// </summary>
        public RelayCommand ButtonCancelCommand { get; set; }

        public RelayCommand ButtonSaveFile { get; set; }

        #endregion

        #region Methods

        private void ProcessFile()
        {
            var cmd = GetJsonCommands();
            Task.Run(() =>
            {
                IsOnProcessing = true;
                var dpClient = new DpClient();
                if (!dpClient.PreProcessData(cmd))
                {
                    IsOnProcessing = false;
                    dpClient.Close();
                    return;
                }

                var csv = dpClient.PeekData(dataFrame: DataFrame.processed_data, type: true);
                dpClient.Close();
                //ProcessedDataTable = DataReader.GetDataTableFromStr(csv);
                //这里获取的DataTable应该带有类型信息
                var stringReader = new StringReader(csv);
                ProcessedDataTable = DataReader.GetDataTableFromCsvOrSR(null, -1, Encoding.UTF8, stringReader);
                stringReader.Dispose();
                IsOnProcessing = false;
            });


            // 旧
            // try
            // {
            //     //写出命令文件，子进程运行环境和主进程一样，写出文件时写到主进程所在目录
            //     File.WriteAllText("commands.txt", GetJsonCommands());
            //     //创建python进程处理数据
            //     IsOnProcessing = true;
            //     _fileProcessingProcess = Process.Start($@"{Directory.GetCurrentDirectory()}/python/cnat_process.exe");
            // }
            // catch (Exception e)
            // {
            //     MessageBox.Show(e.Message);
            //     IsOnProcessing = false;
            // }
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitCommands()
        {
            ButtonProcessCommand = new RelayCommand(ProcessFile);
            ButtonCancelCommand = new RelayCommand(() =>
            {
                var dpClient = new DpClient();
                dpClient.CancelLastTask();
                // try
                // {
                //     _fileProcessingProcess?.Kill();
                // }
                // catch (Exception e)
                // {
                //     MessageBox.Show(e.Message, "取消处理文件");
                // }
                // finally
                // {
                //     IsOnProcessing = false;
                // }
                IsOnProcessing = false;
            });
            ButtonSaveFile = new RelayCommand(() =>
            {
                SaveFileButtonEnabled = false;
                Task.Run(() =>
                {
                    try
                    {
                        var path = $@"{Directory.GetCurrentDirectory()}\{savePath}";
                        var dpClient = new DpClient();
                        if (!dpClient.ExportCsv(path.Replace("\\", "/"), DataFrame.processed_data))
                        {
                            SaveFileButtonEnabled = true;
                            return;
                        }
                        
                        dpClient.Close();
                        path.OpenFileLocation();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "保存文件");
                    }
                    finally
                    {
                        SaveFileButtonEnabled = true;
                    }
                });
            });
        }

        /// <summary>
        /// 生成用于控制python程序进行数据预处理的json格式命令
        /// </summary>
        /// <returns></returns>
        private string GetJsonCommands()
        {
            //获取各类组合选择框选中的列名
            var processAbnormalDataColumns =
                from string columnName in CheckComboBoxAbnormalValues.SelectedItems
                select $"'{columnName}'";
            var standardizedDataColumns =
                from string columnName in CheckComboBoxStandardizedData.SelectedItems
                select $"'{columnName}'";
            var oneHotEncodingColumns =
                from string columnName in CheckComboBoxOneHot.SelectedItems
                select $"'{columnName}'";

            if (string.IsNullOrEmpty(MainViewModel.filePath))
            {
                throw new ArgumentNullException("文件路径", "不合法的文件路径，请尝试加载文件");
            }

            var fileInfo = new FileInfo(MainViewModel.filePath);

            if (!Directory.Exists(@"Temp"))
            {
                Directory.CreateDirectory(@"Temp");
            }


            //构造命令
            var commandStrings = new[]
            {
                $"'path': '{fileInfo.FullName}'",
                $"'save_path': '{savePath}'",
                $"'drop_duplicates': {(IsDropDuplicates ? 1 : 0)}",
                $"'fillna': {MissingValueProcessing}",
                $"'fillna_thresh': {1}",
                $"'process_abnormal_data': {(IsProcessAbnormalValue ? 1 : 0)}",
                $"'process_abnormal_data_columns':[{string.Join(",", processAbnormalDataColumns)}]",
                $"'standardized_data': {StandardizeData}",
                $"'standardized_data_columns': [{string.Join(",", standardizedDataColumns)}]",
                $"'one_hot_encoding': {(IsOntHotEncoding ? 1 : 0)}",
                $"'one_hot_encoding_columns': [{string.Join(",", oneHotEncodingColumns)}]"
            };

            return $"{{{string.Join(",", commandStrings).Replace('\'', '"').Replace('\\', '/')}}}";
        }

        /// <summary>
        /// 开始Tcp客户端，用于与python进程交换信息
        /// </summary>
        private void StartTcpSever()
        {
            var tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 55555);
            //开始监听
            tcpListener.Start();
            //缓冲区
            var bytes = new byte[4096];

            //循环监听
            while (true)
            {
                //接受一个挂起的连接，获取其数据流
                var client = tcpListener.AcceptTcpClient();
                var stream = client.GetStream();

                //读取数据
                var count = stream.Read(bytes, 0, bytes.Length);
                var text = Encoding.UTF8.GetString(bytes, 0, count);

                if (text == "0")
                {
                    //程序正常执行结束，通知读取数据
                    UpdateDataTable();
                    //进程结束
                    _fileProcessingProcess = null;
                    IsOnProcessing = false;
                    MessageBox.Show("处理完成", "数据预处理");
                }
                else
                {
                    IsOnProcessing = false;
                    _fileProcessingProcess = null;
                    //程序出错，显示错误信息
                    MessageBox.Show($"错误：{text}", "数据预处理");
                }

                //关闭客户端
                stream.Dispose();
                client.Dispose();
            }
        }

        /// <summary>
        /// 读取处理后的数据
        /// </summary>
        private void UpdateDataTable()
        {
            var fileInfo = new FileInfo(savePath);
            if (!fileInfo.Exists)
            {
                return;
            }

            ProcessedDataTable = fileInfo.GetDataTableFromCsvOrSR(int.MaxValue, Encoding.UTF8);
        }

        #endregion

        public DataPreProcessingModel(CheckComboBox checkComboBoxAbnormalValues,
            CheckComboBox checkComboBoxStandardizedData,
            CheckComboBox checkBoxOneHot)
        {
            CheckComboBoxAbnormalValues = checkComboBoxAbnormalValues;
            CheckComboBoxStandardizedData = checkComboBoxStandardizedData;
            CheckComboBoxOneHot = checkBoxOneHot;
            //初始化绑定命令
            InitCommands();

            //在新线程开启tcp服务端，接收python进程消息
            //Task.Run(StartTcpSever);
        }
    }
}