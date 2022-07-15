using CsvHelper;
using OfficeOpenXml;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using CsvHelper.Configuration;

namespace ComplexNetworkAnalysisTool.Tools
{
    static class DataReader
    {
        /// <summary>
        /// 读取表格数据
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="extension">扩展名</param>
        /// <param name="encoding">编码</param>
        /// <param name="maxRows">读取的最大行数</param>
        /// <returns></returns>
        public static DataTable GetDataTable(this FileInfo fileInfo, string extension, Encoding encoding,
            int maxRows = int.MaxValue)
        {
            switch (extension.ToLower())
            {
                case ".csv":
                case ".txt":
                    return fileInfo.GetDataTableFromCsvOrSR(maxRows, encoding);
                case ".xls":
                case ".xlsx":
                    return fileInfo.GetDataTableFromExcel(maxRows);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 从CSV文件或是一个字符串流中读取数据，转换为DataTable
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="maxRows">读取的最大行数</param>
        /// <returns></returns>
        public static DataTable GetDataTableFromCsvOrSR(this FileInfo fileInfo, int maxRows, Encoding encoding,
            StringReader reader = null)
        {
            if (maxRows == -1)
            {
                maxRows = int.MaxValue - 1;
            }

            var dataTable = new DataTable();
            try
            {
                var csvConfiguration = new CsvConfiguration(CultureInfo.CurrentCulture)
                {
                    MissingFieldFound = null
                };

                CsvReader csvReader;
                if (reader == null)
                {
                    var streamReader = new StreamReader(fileInfo.FullName, encoding);
                    csvReader = new CsvReader(streamReader, csvConfiguration);
                }
                else
                {
                    csvReader = new CsvReader(reader, csvConfiguration);
                }

                // var streamReader = new StreamReader(fileInfo.FullName, encoding);
                // var csvReader = new CsvReader(streamReader, CultureInfo.CurrentCulture);

                //读取标题行
                csvReader.Read();
                foreach (var s in csvReader.Parser.Record)
                {
                    dataTable.Columns.Add(s?.Split('$')[0].Replace('.', '-'), s.GetTypeFromStr());
                }

                //读取数据行
                var count = 0;
                while (csvReader.Read())
                {
                    var row = dataTable.NewRow();
                    for (var j = 0; j < csvReader.Parser.Count; j++)
                    {
                        if ((dataTable.Columns[j].DataType == typeof(double) || dataTable.Columns[j].DataType == typeof(int))
                            && csvReader.Parser.Record[j].Trim() == "")
                        {
                            row[j] = 0;
                        }
                        else
                        {
                            row[j] = csvReader.Parser.Record[j];
                        }
                    }

                    dataTable.Rows.Add(row);
                    ++count;
                    if (count >= maxRows)
                    {
                        break;
                    }
                }

                csvReader.Dispose();
                //dataTable.Columns["Column1"].ColumnName = "Index_";
                return dataTable;
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("找不到文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException)
            {
                MessageBox.Show("无法读取文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (BadDataException)
            {
                MessageBox.Show("无效的CSV文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("不支持的表格结构", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // catch (ArgumentException)
            // {
            //     MessageBox.Show("数值存在空缺，请选择填补空缺值", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            // }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        /// <summary>
        /// 从CSV文件中读取数据，转换为DataTable
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="maxRows">读取的最大行数</param>
        /// <returns></returns>
        public static DataTable GetDataTableFromExcel(this FileInfo fileInfo, int maxRows)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var dataTable = new DataTable();
            try
            {
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rows = worksheet.Dimension.End.Row;
                    var cols = worksheet.Dimension.End.Column;

                    if (maxRows != int.MaxValue)
                    {
                        rows = Math.Min(rows, maxRows + 1);
                    }

                    for (int j = 1; j <= cols; j++)
                    {
                        var s = worksheet.Cells[1, j].Value?.ToString();
                        dataTable.Columns.Add(s?.Split('$')[0].Replace('.', '_'), s.GetTypeFromStr());
                    }

                    for (int i = 2; i <= rows; i++)
                    {
                        var row = dataTable.NewRow();
                        for (int j = 1; j <= cols; j++)
                        {
                            row[j - 1] = worksheet.Cells[i, j].Value?.ToString();
                        }

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("不支持的表格结构", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("找不到文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException)
            {
                MessageBox.Show("无法读取文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (BadDataException)
            {
                MessageBox.Show("无效的Excel文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException)
            {
                MessageBox.Show("数值或日期存在空缺，请选择填补空缺值", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        /// <summary>
        /// 调用重写后的python服务端进行处理
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="maxRows"></param>
        /// <returns></returns>
        public static DataTable GetDataTable(this FileInfo fileInfo, int maxRows,string encoding="utf-8")
        {
            var dpClient = new DpClient();
            if (!dpClient.LoadData(fileInfo.FullName,encoding))
            {
                return null;
            }
            var csv = dpClient.PeekData(maxRows);
            dpClient.Close();
            return GetDataTableFromStr(csv);
        }

        /// <summary>
        /// 从字符串解析csv文件生成DataTable
        /// </summary>
        /// <param name="csv"></param>
        /// <returns></returns>
        public static DataTable GetDataTableFromStr(string csv)
        {
            var dataTable = new DataTable();
            var csvConfiguration = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                MissingFieldFound = null
            };

            using (var stringReader = new StringReader(csv))
            using (var csvReader = new CsvReader(stringReader, csvConfiguration))
            using (var csvDataReader = new CsvDataReader(csvReader))
            {
                dataTable.Load(csvDataReader);
            }

            //dataTable.Columns["Column1"].ColumnName = "Index_";
            return dataTable;
        }
    }
}