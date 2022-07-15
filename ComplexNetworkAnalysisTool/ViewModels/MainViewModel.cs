using ComplexNetworkAnalysisTool.Tools;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using CefSharp.Wpf;
using ComplexNetworkAnalysisTool.Views;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using MessageBox = HandyControl.Controls.MessageBox;

namespace ComplexNetworkAnalysisTool.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        #region Private Variable

        public static string filePath = string.Empty;
        private DataTable _userDataTable = null;
        private string _dataTableSizeText = "N/A";
        private string _dataTableRowNumText = "N/A";
        private string _dataTableColNumText = "N/A";
        private string _loadingProgressBarState = "Hidden";

        private bool _loadingButtonEnabled = true;

        //上一次成功加载的文件路径
        private string _lastAccessFilePath = string.Empty;
        public static List<string> _dataTableColumnsName;
        private string _fileEncoding = "UTF-8";
        private int _maxRowsNum = int.MaxValue;
        private string _fileName = "未加载数据";

        #endregion

        #region Binding Properties

        /// <summary>
        /// 表格文件全绝对路径
        /// </summary>
        public string FilePath
        {
            get => filePath;
            set
            {
                // 设置旧文件路径
                LastFilePath = filePath;
                filePath = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 用户数据表
        /// </summary>
        public DataTable UserDataTable
        {
            get => _userDataTable;
            set
            {
                _userDataTable = value;
                UpdateDataColumnsName();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 数据表大小提示文本
        /// </summary>
        public string DataTableSizeText
        {
            get => _dataTableSizeText;
            set
            {
                _dataTableSizeText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 数据表行数提示文本
        /// </summary>
        public string DataTableRowNumText
        {
            get => _dataTableRowNumText;
            set
            {
                _dataTableRowNumText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 数据表列数提示文本
        /// </summary>
        public string DataTableColNumText
        {
            get => _dataTableColNumText;
            set
            {
                _dataTableColNumText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 加载文件进度条状态/隐藏/显示
        /// </summary>
        public string LoadingProgressBarState
        {
            get => _loadingProgressBarState;
            set
            {
                _loadingProgressBarState = value;
                RaisePropertyChanged();
            }
        }

        public bool LoadingButtonEnabled
        {
            get => _loadingButtonEnabled;
            set
            {
                _loadingButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 上一个文件的路径名
        /// </summary>
        public string LastFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 数据表列名集合
        /// </summary>
        public List<string> DataTableColumnsName
        {
            get => _dataTableColumnsName;
            set
            {
                _dataTableColumnsName = value;
                if (GraphVisViewModel != null)
                {
                    GraphVisViewModel.VertexSizeBindingList = new List<string>
                    {
                        "_默认_", "_度_", "_介数_", "_紧密中心性_"
                    };
                    GraphVisViewModel.VertexSizeBindingList.AddRange(_dataTableColumnsName);

                    GraphVisViewModel.VertexColorBindingList = new List<string>
                    {
                        "_默认_", "_随机_", "_度_", "_介数_", "_紧密中心性_"
                    };
                    GraphVisViewModel.VertexColorBindingList.AddRange(_dataTableColumnsName);

                    GraphVisViewModel.VertexLabelBindingList = new List<string>
                    {
                        "_默认_"
                    };
                    GraphVisViewModel.VertexLabelBindingList.AddRange(_dataTableColumnsName);
                }

                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 文件编码
        /// </summary>
        public string FileEncoding
        {
            get => _fileEncoding;
            set
            {
                _fileEncoding = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 要预览的最大数据行数
        /// </summary>
        public int MaxRowsNum
        {
            get => _maxRowsNum;
            set
            {
                _maxRowsNum = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 用于数据预处理页面的模型
        /// </summary>
        public DataPreProcessingModel DataPreProcessingPage { get; set; }

        /// <summary>
        /// 用于相似度计算页面的模型
        /// </summary>
        public SimilarityCalculationModel SimilarityCalculationModel { get; set; }

        public GraphVisViewModel GraphVisViewModel { get; set; }

        #endregion

        #region Binding Commands

        public RelayCommand TestCommand { get; set; }

        /// <summary>
        /// 打开选择文件对话框
        /// </summary>
        public RelayCommand OpenFileDialogCommand { get; set; }

        /// <summary>
        /// 导入数据
        /// </summary>
        public RelayCommand ImportDataCommand { get; set; }


        public RelayCommand ButtonOpenFileLocation { get; set; }

        public RelayCommand GetDataReportCommand { get; set; }

        #endregion


        #region Methords

        /// <summary>
        /// 初始化所有绑定命令
        /// </summary>
        private void InitCommands()
        {
            TestCommand = new RelayCommand(() => { MessageBox.Show("Test"); });

            OpenFileDialogCommand = new RelayCommand(() =>
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
                    FilePath = openFileDialog.FileName;
                }
            });

            ImportDataCommand = new RelayCommand(ImportDataTable);

            ButtonOpenFileLocation = new RelayCommand(() =>
            {
                $@"{Directory.GetCurrentDirectory()}\Temp\DataReport.html".OpenFileLocation();
            });

            GetDataReportCommand = new RelayCommand(() =>
            {
                var reportWindow = new DataReportWindow();
                reportWindow.Show();
            });
        }

        /// <summary>
        /// 从文件读取数据
        /// </summary>
        private void ImportDataTable()
        {
            if (string.IsNullOrEmpty(FilePath)) return;
            var fileInfo = new FileInfo(FilePath);
            if (!fileInfo.Exists)
            {
                MessageBox.Show("文件不存在，请检查路径后重试", "提示", MessageBoxButton.OK);
                return;
            }

            //计算文件大小,保留两位小数
            var fileSize = fileInfo.Length <= 1024 * 1024
                ? $"{fileInfo.Length / 1024.0:F2}KB"
                : $"{fileInfo.Length / (1024.0 * 1024):F2}MB";
            DataTableSizeText = fileSize;

            //根据扩展名加载DataTable
            var extension = fileInfo.Extension.ToLower();
            //读取数据
            if (extension == ".csv" || extension == ".xls" || extension == ".xlsx" || extension == ".txt")
            {
                //在新的线程读取数据
                Task.Run(() =>
                {
                    //显示进度条,禁用加载按钮
                    LoadingProgressBarState = "Visible";
                    LoadingButtonEnabled = false;

                    //读取数据获得DataTable,旧
                    // var dataTable =
                    //     fileInfo.GetDataTable(extension, Encoding.GetEncoding(FileEncoding),
                    //         MaxRowsNum); 
                    var dataTable = fileInfo.GetDataTable(MaxRowsNum,FileEncoding);


                    if (dataTable != null)
                    {
                        UserDataTable = dataTable;
                        DataTableRowNumText = $"{UserDataTable?.Rows.Count - 1}行";
                        DataTableColNumText = $"{UserDataTable?.Columns.Count - 1}列";
                        FileName = FilePath.Split('/', '\\').Last();
                        //_lastAccessFilePath = FilePath;
                    }
                    else
                    {
                        // 读取数据失败
                        // 恢复上一个文件名
                        FilePath = LastFilePath;
                    }

                    //隐藏进度条,启用按钮
                    LoadingProgressBarState = "Hidden";
                    LoadingButtonEnabled = true;
                });
            }
            else
            {
                MessageBox.Show("不支持的文件格式", "提示", MessageBoxButton.OK);
                return;
            }
        }

        private void UpdateDataColumnsName()
        {
            DataTableColumnsName = (from DataColumn dataColumn in UserDataTable.Columns select dataColumn.ColumnName)
                .ToList();
        }

        #endregion

        public MainViewModel(CheckComboBox checkComboBoxAbnormalValues, CheckComboBox checkComboBoxStandardizedData,
            CheckComboBox checkBoxOneHot, CheckComboBox checkComboBoxSimilarityCalculation, ChromiumWebBrowser browser)
        {
            InitCommands();
            DataPreProcessingPage = new DataPreProcessingModel(checkComboBoxAbnormalValues,
                checkComboBoxStandardizedData,
                checkBoxOneHot);
            SimilarityCalculationModel = new SimilarityCalculationModel(checkComboBoxSimilarityCalculation);
            GraphVisViewModel = new GraphVisViewModel(browser);
        }
    }
}