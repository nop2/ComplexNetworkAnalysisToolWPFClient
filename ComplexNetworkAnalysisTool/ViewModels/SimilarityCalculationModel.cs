using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using ComplexNetworkAnalysisTool.Tools;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HandyControl.Controls;
using MessageBox = HandyControl.Controls.MessageBox;

namespace ComplexNetworkAnalysisTool.ViewModels
{
    public class SimilarityCalculationModel : ViewModelBase
    {
        private DataTable _dataTableFilter;

        //用于界面绑定的距离数据表
        public static DataTable dataTableDistanceFilter;

        //上一次成功筛选距离表的搜索字符串
        public static string lastDtDistSearchPattern = "";
        //上一次成功筛选数据表的搜索字符串
        private string lastDtSearchPattern = "";

        //原始距离数据表，用于筛选数据
        private DataTable _dataTableDistance;
        private string _searchPattern = string.Empty;
        public static string _searchPatternDistance = "dist<1";
        private List<string> _dataTableColumnName = null;
        private bool _isCreateIndexColumn = false;
        private string _label = string.Empty;
        private int _dtFilterRowCount = 0;
        private int _dtDistRowCount = 0;
        private string _distanceType = "euclidean";
        private bool _isButtonEnabled = true;

        #region Properties

        /// <summary>
        /// 根据条件查询出的数据表
        /// </summary>
        public DataTable DataTableFilter
        {
            get => _dataTableFilter;
            set
            {
                _dataTableFilter = value;
                if (_dataTableFilter != null)
                {
                    UpdateDataColumnsName();
                    DtFilterRowCount = _dataTableFilter.Rows.Count;
                }
                else
                {
                    DtFilterRowCount = 0;
                }

                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 查询表达式
        /// </summary>
        public string SearchPattern
        {
            get => _searchPattern;
            set
            {
                _searchPattern = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 数据表列名集合
        /// </summary>
        public List<string> DataTableColumnName
        {
            get => _dataTableColumnName;
            set
            {
                _dataTableColumnName = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 是否创建序号列作为主键
        /// </summary>
        public bool IsCreateIndexColumn
        {
            get => _isCreateIndexColumn;
            set
            {
                _isCreateIndexColumn = value;
                RaisePropertyChanged();
            }
        }

        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 用于计算相似度的数据表行数
        /// </summary>
        public int DtFilterRowCount
        {
            get => _dtFilterRowCount;
            set
            {
                _dtFilterRowCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 距离数据表
        /// </summary>
        public DataTable DataTableDistanceFilter
        {
            get => dataTableDistanceFilter;
            set
            {
                dataTableDistanceFilter = value;
                DtDistRowCount = dataTableDistanceFilter?.Rows.Count ?? 0;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 距离数据表行数
        /// </summary>
        public int DtDistRowCount
        {
            get => _dtDistRowCount;
            set
            {
                _dtDistRowCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 距离表筛选条件
        /// </summary>
        public string SearchPatternDistance
        {
            get => _searchPatternDistance;
            set
            {
                _searchPatternDistance = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 距离定义
        /// </summary>
        public string DistanceType
        {
            get => _distanceType;
            set
            {
                _distanceType = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 按钮是否有效
        /// </summary>
        public bool IsButtonEnabled
        {
            get => _isButtonEnabled;
            set
            {
                _isButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region UI Contorls

        public CheckComboBox CheckComboBoxSimilarityCalculation { get; set; }

        #endregion

        #region Commands

        /// <summary>
        /// 筛选用于计算相距离的数据
        /// </summary>
        public RelayCommand<string> SearchCommand { get; set; }

        /// <summary>
        /// 筛选距离表中的数据
        /// </summary>
        public RelayCommand<string> DtDistanceSearchCommand { get; set; }


        /// <summary>
        /// 距离计算
        /// </summary>
        public RelayCommand DistanceCalculateCommand { get; set; }

        /// <summary>
        /// 距离表筛选数据
        /// </summary>
        public RelayCommand DistanceSearchCommand { get; set; }

        public RelayCommand CalculateCancelCommand { get; set; }

        #endregion

        #region Methords

        private void UpdateDataColumnsName()
        {
            DataTableColumnName = (from DataColumn dataColumn in DataTableFilter.Columns select dataColumn.ColumnName)
                .ToList();
        }


        /// <summary>
        /// 初始化绑定命令
        /// </summary>
        private void InitCommands()
        {
            SearchCommand = new RelayCommand<string>(searchPattern =>
            {
                if (searchPattern.Contains("Index_"))
                {
                    MessageBox.Show("未找到列[Index_]");
                    return;
                }

                var table = DataPreProcessingModel.processedDataTable;

                if (table == null)
                {
                    MessageBox.Show("未加载数据", "相似度计算");
                    return;
                }

                //获取要计算的列
                var selectedColumns = (from string columnName in CheckComboBoxSimilarityCalculation.SelectedItems
                    select columnName).ToList();
                if (selectedColumns.Count == 0)
                {
                    MessageBox.Show("请选择至少一个用于计算相似度的列", "相似度计算");
                    return;
                }

                string[] columns = null;


                if (Label != "Index_")
                {
                    columns = new string[selectedColumns.Count + 1];
                    columns[0] = Label;
                    selectedColumns.CopyTo(columns, 1);
                }
                else
                {
                    columns = new string[selectedColumns.Count];
                    selectedColumns.CopyTo(columns);
                }


                try
                {
                    searchPattern = Regex.Replace(searchPattern, "[\"“”‘’]+", "'");
                    var rows = table.Select(searchPattern);
                    if (rows.Length == 0)
                    {
                        DataTableFilter = null;
                        return;
                    }

                    DataTableFilter = rows.CopyToDataTable().DefaultView.ToTable(false, columns);
                    lastDtSearchPattern = searchPattern;

                    // if (IsCreateIndexColumn)
                    // {
                    //     tempTable.Columns.Add("key", typeof(int));
                    //     for (int i = 0; i < tempTable.Rows.Count; i++)
                    //     {
                    //         tempTable.Rows[i]["key"] = i;
                    //     }
                    // }

                    //var tempTable = table.DefaultView.ToTable(false, columns);
                    //var rows = tempTable.Select(searchPattern);
                    //DataTableFilter = rows.Length == 0 ? null : rows.CopyToDataTable();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "数据查询失败");
                }
            });
            DistanceCalculateCommand = new RelayCommand(() =>
            {
                if (DtFilterRowCount >= 1000)
                {
                    var check = MessageBox.Show("选择计算行数>1000，计算可能会花费大量时间，是否继续?", "", MessageBoxButton.YesNo);
                    if (check == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                var selectedColumns = (from string columnName in CheckComboBoxSimilarityCalculation.SelectedItems
                    select $"[{columnName}]").ToList();


                Task.Run(() =>
                {
                    try
                    {
                        IsButtonEnabled = false;

                        var searchPattern = Regex.Replace(lastDtSearchPattern, "[\"“”‘’]+", "'");
                        var columns = string.Join(",", selectedColumns);

                        var sql = $"select [-0ID0-],{columns} from processed_data where {searchPattern};";
                        if (searchPattern.Trim() == "")
                        {
                            sql = $"select [-0ID0-],{columns} from processed_data;";
                        }

                        var dpClient = new DpClient();
                        if (!dpClient.FilteringData(sql))
                        {
                            return;
                        }

                        if (!dpClient.CalculateSimilarity(DistanceType))
                        {
                            return;
                        }

                        var csvStr = dpClient.PeekData(dataFrame: DataFrame.distance_data, type: true);
                        dpClient.Close();

                        var sr = new StringReader(csvStr);

                        _dataTableDistance = DataReader.GetDataTableFromCsvOrSR(null, -1, Encoding.UTF8, sr);
                        DataTableDistanceFilter = _dataTableDistance;
                        sr.Dispose();

                        // _dataTableDistance =
                        //     DataTableFilter.Distance(IsCreateIndexColumn ? DataTableFilter.Columns.Count - 1 : 0,
                        //         DistanceType);
                        // DataTableDistanceFilter = _dataTableDistance;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "相似度计算");
                    }
                    finally
                    {
                        IsButtonEnabled = true;
                    }
                });
            });
            DtDistanceSearchCommand = new RelayCommand<string>(searchPattern =>
            {
                if (searchPattern.Contains("Index_"))
                {
                    MessageBox.Show("未找到列[Index_]");
                    return;
                }

                if (_dataTableDistance == null)
                {
                    MessageBox.Show("数据表为空或未加载数据表", "数据筛选");
                    return;
                }

                try
                {
                    var rows = _dataTableDistance.Select(searchPattern);
                    if (rows.Length == 0)
                    {
                        DataTableDistanceFilter = null;
                        return;
                    }

                    DataTableDistanceFilter = rows.CopyToDataTable();
                    lastDtDistSearchPattern = searchPattern;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "数据筛选");
                }
            });
            CalculateCancelCommand = new RelayCommand(() =>
            {
                var dpClient = new DpClient(); 
                dpClient.CancelLastTask();
                //dpClient.Close();
                IsButtonEnabled = true;
            });
        }

        #endregion


        public SimilarityCalculationModel(CheckComboBox checkComboBoxSimilarityCalculation)
        {
            CheckComboBoxSimilarityCalculation = checkComboBoxSimilarityCalculation;
            InitCommands();
        }
    }
}