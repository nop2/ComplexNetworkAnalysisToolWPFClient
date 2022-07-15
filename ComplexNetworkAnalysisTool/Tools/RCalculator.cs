using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Controls;
using MaterialDesignThemes.Wpf;
using RDotNet;

namespace ComplexNetworkAnalysisTool.Tools
{
    public static class RCalculator
    {
        /// <summary>
        /// 计算距离
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="keyIndex"></param>
        /// <returns></returns>
        public static DataTable Distance(this DataTable dataTable, int keyIndex, string distanceType)
        {
            if (dataTable == null)
            {
                return null;
            }

            //初始化R引擎
            var engine = MainWindow.Engine;

            //先将datatable中向量转换为R矩阵
            var rowNum = dataTable.Rows.Count;
            var colNum = dataTable.Columns.Count - 1;

            var m = engine.CreateNumericMatrix(rowNum, colNum);
            for (int i = 0; i < rowNum; i++)
            {
                if (keyIndex == 0)
                {
                    for (int j = 1; j < colNum; j++)
                    {
                        m[i, j - 1] = double.Parse(dataTable.Rows[i][j].ToString());
                    }
                }
                else
                {
                    for (int j = 0; j < colNum - 1; j++)
                    {
                        m[i, j] = double.Parse(dataTable.Rows[i][j].ToString());
                    }
                }
            }

            //调用R函数计算距离，返回值整理为向量
            engine.SetSymbol("m", m);
            var dis = engine.Evaluate($"c(dist(m,method='{distanceType}'))").AsNumeric();
            //gc
            engine.Evaluate("rm(list=ls())");
            engine.Evaluate("gc()");

            //构建距离矩阵的datatable
            var table = new DataTable();
            table.Columns.Add("From", typeof(string));
            table.Columns.Add("To", typeof(string));
            table.Columns.Add("Metric", typeof(double));

            int count = 0;
            for (int i = 0; i < rowNum; i++)
            {
                for (int j = i + 1; j < rowNum; j++)
                {
                    var dataRow = table.NewRow();
                    dataRow[0] = dataTable.Rows[i][keyIndex];
                    dataRow[1] = dataTable.Rows[j][keyIndex];
                    dataRow[2] = dis[count++];
                    table.Rows.Add(dataRow);
                }
            }


            return table;
        }

    }
}