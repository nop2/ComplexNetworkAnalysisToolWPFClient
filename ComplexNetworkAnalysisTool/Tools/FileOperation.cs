using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp.DevTools.SystemInfo;
using HandyControl.Controls;

namespace ComplexNetworkAnalysisTool.Tools
{
    public static class FileOperation
    {
        /// <summary>
        /// 打开文件所在位置并选中文件
        /// </summary>
        /// <param name="fileName"></param>
        public static void OpenFileLocation(this string fileName)
        {
            if (!Directory.Exists(fileName) && !File.Exists(fileName))
            {
                MessageBox.Show("文件不存在");
                return;
            }
            Process.Start("explorer.exe", $"/e,/select,{fileName}");
        }
           
        
    }
}