using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CefSharp;
using CefSharp.Wpf;
using ComplexNetworkAnalysisTool.Models;
using ComplexNetworkAnalysisTool.Tools;
using ComplexNetworkAnalysisTool.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace ComplexNetworkAnalysisTool.ViewModels
{
    class GraphVisViewModel : ViewModelBase
    {
        private bool _isPageSelected;
        private int _lastDataTableHash = 0;
        public static List<string> _vertexSizeBindingList;
        public static List<string> _vertexColorBindingList;
        public static List<string> _vertexLabelBindingList;
        private int _isHtml = 1;
        private string _vertexSize;
        private string _vertexColor;
        private string _edgeWeight;
        private string _layout;
        private bool _isUiEnabled = true;
        private string _vertexLabel;
        private string _browserAddress = "";
        private Graph _graphModel;
        private bool _isGraphInfoExpand;
        private int _startId;
        private int _endId;
        private string _vertexShape;
        private string _isColorPickerShow = "Collapsed";
        private string _colorPickerColor = "#FFF44336";

        #region Properties

        /// <summary>
        /// 节点数据表
        /// </summary>
        public DataTable VertexDataTable { get; set; }

        private ChromiumWebBrowser ChromiumWebBrowser { get; set; } = null;

        public Graph GraphModel
        {
            get => _graphModel;
            set
            {
                _graphModel = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 当切换到这个页面时要做的事情
        /// </summary>
        public bool IsPageSelected
        {
            get => _isPageSelected;
            set
            {
                _isPageSelected = value;


                try
                {
                    if (SimilarityCalculationModel.dataTableDistanceFilter == null)
                    {
                        return;
                    }

                    var hash = SimilarityCalculationModel.dataTableDistanceFilter.GetHashCode();
                    if (_isPageSelected && _lastDataTableHash != hash)
                    {
                        _lastDataTableHash = hash;
                        Task.Run(() =>
                        {
                            var dpClient = new DpClient();
                            var sql =
                                $"select * from distance_data where {SimilarityCalculationModel.lastDtDistSearchPattern};";
                            if (SimilarityCalculationModel.lastDtDistSearchPattern.Trim() == "")
                            {
                                sql = "select * from distance_data;";
                            }

                            dpClient.FilteringData(sql);
                            dpClient.Close();
                        });
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "距离数据筛选");
                }
            }
        }

        public bool IsUiEnabled
        {
            get => _isUiEnabled;
            set
            {
                _isUiEnabled = value;
                RaisePropertyChanged();
            }
        }

        public List<string> VertexSizeBindingList
        {
            get => _vertexSizeBindingList;
            set
            {
                _vertexSizeBindingList = value;
                RaisePropertyChanged();
            }
        }

        public List<string> VertexColorBindingList
        {
            get => _vertexColorBindingList;
            set
            {
                _vertexColorBindingList = value;
                RaisePropertyChanged();
            }
        }

        public List<string> VertexLabelBindingList
        {
            get => _vertexLabelBindingList;
            set
            {
                _vertexLabelBindingList = value;
                RaisePropertyChanged();
            }
        }

        public List<string> EdgeWeightBindingList { get; set; } = new List<string> { "_默认_", "dist", "weight" };

        public string BrowserAddress
        {
            get => _browserAddress;
            set
            {
                _browserAddress = value;
                RaisePropertyChanged();
            }
        }

        //最短路径计算
        public int StartId
        {
            get => _startId;
            set
            {
                _startId = value;
                RaisePropertyChanged();
            }
        }

        public int EndId
        {
            get => _endId;
            set
            {
                _endId = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region GraphVis

        public bool IsGraphInfoExpand
        {
            get => _isGraphInfoExpand;
            set
            {
                _isGraphInfoExpand = value;
                RaisePropertyChanged();
            }
        }

        public int IsHtml
        {
            get => _isHtml;
            set
            {
                _isHtml = value;
                RaisePropertyChanged();
            }
        }

        public string VertexSize
        {
            get => _vertexSize;
            set
            {
                _vertexSize = value;
                RaisePropertyChanged();
            }
        }

        public string VertexColor
        {
            get => _vertexColor;
            set
            {
                _vertexColor = value;
                RaisePropertyChanged();
                IsColorPickerShow = _vertexColor != "_默认_" ? "Collapsed" : "Visible";
            }
        }

        public string IsColorPickerShow
        {
            get => _isColorPickerShow;
            set
            {
                _isColorPickerShow = value;
                RaisePropertyChanged();
            }
        }


        public string VertexLabel
        {
            get => _vertexLabel;
            set
            {
                _vertexLabel = value;
                RaisePropertyChanged();
            }
        }

        public string EdgeWeight
        {
            get => _edgeWeight;
            set
            {
                _edgeWeight = value;
                RaisePropertyChanged();
            }
        }

        public string Layout
        {
            get => _layout;
            set
            {
                _layout = value;
                RaisePropertyChanged();
            }
        }

        public string VertexShape
        {
            get => _vertexShape;
            set
            {
                _vertexShape = value;
                RaisePropertyChanged();
            }
        }

        public string ColorPickerColor
        {
            get => _colorPickerColor;
            set
            {
                _colorPickerColor = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands

        public RelayCommand BrowserGoBackCommand { get; set; }
        public RelayCommand BrowserGoForwardCommand { get; set; }

        public RelayCommand OpenFileInBrowserCommand { get; set; }
        public RelayCommand OpenFileLocationCommand { get; set; }

        public RelayCommand GenerateNetworkCommand { get; set; }

        public RelayCommand<string> CommunityDetectionCommand { get; set; }

        public RelayCommand ShowDataTableCommand { get; set; }

        public RelayCommand DegreeDistributionCommand { get; set; }

        public RelayCommand ImportDataTableCommand { get; set; }

        public RelayCommand ExportGraphML { get; set; }

        public RelayCommand GetShortestPathCommand { get; set; }
        public RelayCommand ShowColorPickerCommand { get; set; }

        public RelayCommand ShowDegreeDistribution { get; set; }

        #endregion

        #region Methords

        private void InitCommands()
        {
            //生成网络
            GenerateNetworkCommand = new RelayCommand(() =>
            {
                var htmlPath = $"{Directory.GetCurrentDirectory()}\\Temp\\NetWork.html".Replace('\\', '/');
                if (IsHtml == 0)
                {
                    htmlPath = $"{Directory.GetCurrentDirectory()}\\Temp\\NetWork.svg".Replace('\\', '/');
                }

                var color = VertexColor == "_默认_" ? $"#{ColorPickerColor.Substring(3)}" : VertexColor;


                var jsonParams =
                    $"{{\"path\":\"{htmlPath}\",\"isHtml\":{IsHtml},\"vertex_size\": \"{VertexSize}\"," +
                    $"\"vertex_color\": \"{color}\",\"vertex_shape\":\"{VertexShape.Split(' ')[2]}\",\"vertex_label\": \"{VertexLabel}\"," +
                    $"\"edge_weight\": \"{EdgeWeight}\",\"layout\":\"{Layout}\"}}";

                // MessageBox.Show(jsonParams);

                IsUiEnabled = false;

                Task.Run(() =>
                {
                    try
                    {
                        var dpClient = new DpClient();
                        if (!dpClient.GenerateNetwork(jsonParams))
                        {
                            IsUiEnabled = true;
                            return;
                        }

                        BrowserAddress = htmlPath.Replace("#", "%23");
                        GraphModel = dpClient.GetGraphInfo();
                        IsGraphInfoExpand = true;
                        dpClient.Close();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "生成网络");
                    }
                    finally
                    {
                        IsUiEnabled = true;
                    }
                });
            });

            //社团发现
            CommunityDetectionCommand = new RelayCommand<string>(algorithm =>
            {
                IsUiEnabled = false;

                var htmlPath =
                    $"{Directory.GetCurrentDirectory()}\\Temp\\Community_{algorithm}.html".Replace('\\', '/');

                Task.Run(() =>
                {
                    try
                    {
                        var dpClient = new DpClient();
                        var count = dpClient.CommunityDetection(htmlPath, algorithm);
                        if (count != 0)
                        {
                            BrowserAddress = htmlPath.Replace("#", "%23");
                        }

                        dpClient.Close();
                        MessageBox.Show($"发现社团数量：{count}", "社团发现");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "生成网络");
                    }
                    finally
                    {
                        IsUiEnabled = true;
                    }
                });
            });

            //查看数据表
            ShowDataTableCommand = new RelayCommand(() => { new DataTableWindow().Show(); });

            //导入数据
            ImportDataTableCommand = new RelayCommand(() =>
            {
                new ImportDataTableWindow().ShowDialog();
                VertexSizeBindingList = _vertexSizeBindingList;
                VertexColorBindingList = _vertexColorBindingList;
                VertexLabelBindingList = _vertexLabelBindingList;
            });

            GetShortestPathCommand = new RelayCommand(() =>
            {
                if (StartId < 0 || StartId >= GraphModel.VertexCount || EndId < 0 || EndId >= GraphModel.VertexCount)
                {
                    MessageBox.Show("节点ID超出索引", "最短路径");
                    return;
                }

                new ShortestPathWindow(StartId, EndId, VertexLabel).ShowDialog();
            });

            ExportGraphML = new RelayCommand(() =>
            {
                IsUiEnabled = false;
                try
                {
                    Task.Run(() =>
                    {
                        var path = $@"{Directory.GetCurrentDirectory()}\Temp\graph.graphml";
                        var dpClient = new DpClient();
                        if (!dpClient.ExportGraphML(path.Replace("\\", "/")))
                        {
                            IsUiEnabled = true;
                            dpClient.Close();
                            MessageBox.Show("导出失败");
                            return;
                        }

                        dpClient.Close();
                        path.OpenFileLocation();
                    });
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                finally
                {
                    IsUiEnabled = true;
                }
            });

            BrowserGoBackCommand = new RelayCommand(() => { ChromiumWebBrowser.Back(); });

            BrowserGoForwardCommand = new RelayCommand(() => { ChromiumWebBrowser.Forward(); });

            OpenFileInBrowserCommand = new RelayCommand(() =>
            {
                Process.Start($"{Directory.GetCurrentDirectory()}\\Temp\\graph.html");
            });

            OpenFileLocationCommand = new RelayCommand(() =>
            {
                $"{Directory.GetCurrentDirectory()}\\Temp\\".OpenFileLocation();
            });

            ShowColorPickerCommand = new RelayCommand(() =>
            {
                var colorPicker = new ColorPicker();
                colorPicker.confirm += (sender, args) =>
                {
                    var window = (ColorPicker)sender;
                    ColorPickerColor = window.Color;
                };
                colorPicker.ShowDialog();

                // MessageBox.Show(VertexColor);
            });

            ShowDegreeDistribution = new RelayCommand(() =>
            {
                IsUiEnabled = false;

                var htmlPath =
                    $"{Directory.GetCurrentDirectory()}\\Temp\\DegreeDistribution.html".Replace('\\', '/');

                var dispatcher = Dispatcher.CurrentDispatcher;

                Task.Run(() =>
                {
                    try
                    {
                        var dpClient = new DpClient();
                        if (dpClient.ShowDegreeDistribution(htmlPath))
                        {
                            dispatcher.Invoke(() =>
                            {
                                new DataReportWindow("degree").Show();
                            });
                        }
                        else
                        {
                            MessageBox.Show("生成失败", "度分布");
                        }


                        dpClient.Close();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "度分布");
                    }
                    finally
                    {
                        IsUiEnabled = true;
                    }
                });
            });
        }

        #endregion

        public GraphVisViewModel(ChromiumWebBrowser browser)
        {
            ChromiumWebBrowser = browser;
            GraphModel = new Graph();
            InitCommands();

            ChromiumWebBrowser.PreviewMouseWheel += (sender, args) =>
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;
                try
                {
                    if (args.Delta > 0)
                    {
                        ChromiumWebBrowser.ZoomInCommand.Execute(null);
                    }
                    else if (args.Delta < 0)
                    {
                        ChromiumWebBrowser.ZoomOutCommand.Execute(null);
                    }

                    args.Handled = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("放缩失败");
                }
            };
        }
    }
}