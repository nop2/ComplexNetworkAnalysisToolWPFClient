using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ComplexNetworkAnalysisTool.Models;
using HandyControl.Controls;
using Newtonsoft.Json;

namespace ComplexNetworkAnalysisTool.Tools
{
    public enum DataFrame
    {
        data = 0,
        processed_data = 1,
        processed_data_filter = 2,
        distance_data = 3,
        distance_data_filter = 4,
        vertex_data = 5,
        edge_data = 6
    }

    class DpClient
    {
        public int Port { get; set; }
        private static string _ip = "127.0.0.1";
        private TcpClient _client = null;
        private NetworkStream _networkStream = null;

        /// <summary>
        /// python服务端中的dataframes列表
        /// </summary>
        public static string[] DataFrames { get; } =
        {
            "data", "processed_data", "processed_data_filter",
            "distance_data", "distance_data_filter","vertex_data","edge_data"
        };

        /// <summary>
        /// 通知服务端读取表格数据
        /// </summary>
        /// <param name="path">表格路径</param>
        public bool LoadData(string path,string encoding = "utf-8")
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"LoadData\",\"len\":\"0\",\"params\":\"{path.Replace("\\", "/")}?{encoding}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);
            Console.WriteLine(cmd);

            //读取服务端响应数据
            return GetResponse() == "1";
        }

        /// <summary>
        /// 从原始表格获得指定行数的数据，csv文本格式，逗号分割
        /// </summary>
        /// <param name="maxLines">最大行数</param>
        /// <param name="dataFrame">要读取的文件</param>
        /// <returns></returns>
        public string PeekData(int maxLines = -1, DataFrame dataFrame = DataFrame.data, bool type = false)
        {
            //向服务端发送命令
            var t = type ? 1 : 0;
            var cmd =
                $"{{\"cmd\":\"PeekData\",\"len\":\"0\",\"params\":\"{t}?{maxLines}?{DataFrames[(int) dataFrame]}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);
            //Console.WriteLine(cmd);

            //接收服务端通告的数据字节数
            var lenBuffer = new byte[1024];
            _networkStream.Read(lenBuffer, 0, lenBuffer.Length);
            var dataSize = int.Parse(Encoding.UTF8.GetString(lenBuffer));
            // Console.WriteLine($"Sever: {dataSize}");

            if (dataSize == 0)
            {
                return null;
            }

            //响应服务端,区分两次发送的数据
            _networkStream.WriteByte(1);

            //接收服务端发送的数据
            var dataBuffer = new byte[dataSize];
            _networkStream.Read(dataBuffer, 0, dataBuffer.Length);
            var str = Encoding.UTF8.GetString(dataBuffer);
            // Console.WriteLine($"Sever:\n{str}");
            return str;
        }

        /// <summary>
        /// 生成数据分析报告
        /// </summary>
        /// <param name="savePath">文件保存路径</param>
        public bool GetReport(string savePath)
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"GetReport\",\"len\":\"0\",\"params\":\"{savePath.Replace("\\", "/")}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);
            // Console.WriteLine(cmd);

            //读取服务端响应数据,缓冲区大于1会导致接收到的字符串多空白字符
            return GetResponse() == "1";
        }

        /// <summary>
        /// 筛选数据
        /// </summary>
        /// <param name="sql">sql查询语句</param>
        /// <param name="dataFrame"></param>
        public bool FilteringData(string sql)
        {
            // cmd = {
            //     "cmd": "FilteringData",
            //     "len": "0",
            //     "params": "select * from data where a>10;"}
            // env = {
            //     'data': self.data,
            //     'processed_data': self.processed_data,
            //     'processed_data_filter': self.processed_data_filter,
            //     'distance_data': self.distance_data,
            //     'distance_data_filter': self.distance_data_filter
            // }

            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"FilteringData\",\"len\":\"0\",\"params\":\"{sql}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);
            // Console.WriteLine(cmd);

            //读取服务端响应数据
            return GetResponse() == "1";
        }

        /// <summary>
        /// 预处理数据
        /// </summary>
        /// <param name="paramsDict"></param>
        public bool PreProcessData(string paramsDict)
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"PreProcessData\",\"len\":\"0\",\"params\":{paramsDict}}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);
            // Console.WriteLine(cmd);

            //读取服务端响应数据
            return GetResponse() == "1";
        }


        public bool CalculateSimilarity(string metric = "euclidean")
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"CalculateSimilarity\",\"len\":\"0\",\"params\":\"{metric}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            //读取服务端响应数据
            return GetResponse() == "1";
        }

        public bool ExportCsv(string path, DataFrame dataFrame)
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"ExportCsv\",\"len\":\"0\",\"params\":\"{path}?{DataFrames[(int) dataFrame]}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            //读取服务端响应数据
            return GetResponse() == "1";
        }

        public bool ExportGraphML(string path)
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"ExportGraphML\",\"len\":\"0\",\"params\":\"{path}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            //读取服务端响应数据
            return GetResponse() == "1";
        }

        /// <summary>
        /// 通过输入json格式参数生成网络
        /// </summary>
        /// <returns></returns>
        public bool GenerateNetwork(string jsonParams)
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"GenerateNetwork\",\"len\":\"0\",\"params\":{jsonParams}}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            return GetResponse() == "1";
        }

        public bool ImportData(string edgePath,string vertexPath,string relatePath,bool isDirected,bool isOverlay,out string columns)
        {
            //向服务端发送命令
            var directed = isDirected ? 1 : 0;
            var overlay = isOverlay ? 1 : 0;
            var cmd = $"{{\"cmd\":\"ImportData\",\"len\":\"0\",\"params\":\"{edgePath}?{vertexPath}?{relatePath}?{directed}?{overlay}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            //接收服务端通告的数据字节数
            var lenBuffer = new byte[1024];
            _networkStream.Read(lenBuffer, 0, lenBuffer.Length);
            var dataSize = int.Parse(Encoding.UTF8.GetString(lenBuffer));
            // Console.WriteLine($"Sever: {dataSize}");
            if (dataSize == 0)
            {
                columns = null;
                return false;
            }

            //响应服务端,区分两次发送的数据
            _networkStream.WriteByte(1);

            //接收服务端发送的数据
            
            if (dataSize != -1)
            {
                var dataBuffer = new byte[dataSize];
                _networkStream.Read(dataBuffer, 0, dataBuffer.Length);
                columns = Encoding.UTF8.GetString(dataBuffer);
                _networkStream.WriteByte(1);

            }
            else
            {
                columns = null;
            }

            return GetResponse() == "1";
        }

        /// <summary>
        /// 导出度分布图
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool ShowDegreeDistribution(string path)
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"ShowDegreeDistribution\",\"len\":\"0\",\"params\":\"{path.Replace('\\', '/')}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            return GetResponse() == "1";
        }

        /// <summary>
        /// 获得图的描述信息
        /// </summary>
        /// <returns></returns>
        public Graph GetGraphInfo()
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"GetGraphInfo\",\"len\":\"0\",\"params\":\"\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            //接收服务端通告的数据字节数
            var lenBuffer = new byte[128];
            _networkStream.Read(lenBuffer, 0, lenBuffer.Length);
            var dataSize = int.Parse(Encoding.UTF8.GetString(lenBuffer));

            //响应服务端,区分两次发送的数据
            _networkStream.WriteByte(1);

            //接收服务端发送的数据
            var dataBuffer = new byte[dataSize];
            _networkStream.Read(dataBuffer, 0, dataBuffer.Length);
            var str = Encoding.UTF8.GetString(dataBuffer);

            // File.WriteAllText("GraphInfo.txt",str);

            //反序列化生成图对象
            var graph = JsonConvert.DeserializeObject<Graph>(str);
            // MessageBox.Show(graph.AvgDegree.ToString());

            return graph;
        }

        public int CommunityDetection(string path, string algorithm)
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"CommunityDetection\",\"len\":\"0\",\"params\":\"{path}?{algorithm}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            //接收服务端通告的社团数量
            var lenBuffer = new byte[64];
            _networkStream.Read(lenBuffer, 0, lenBuffer.Length);
            var count = int.Parse(Encoding.UTF8.GetString(lenBuffer));

            return count;
        }

        public string GetShortestPath(int startId, int endId, string label)
        {
            //向服务端发送命令
            var cmd = $"{{\"cmd\":\"GetShortestPath\",\"len\":\"0\",\"params\":\"{startId}?{endId}?{label}\"}}";
            var bytes = Encoding.UTF8.GetBytes(cmd);
            _networkStream.Write(bytes, 0, bytes.Length);

            //接收服务端通告的数据字节数
            var lenBuffer = new byte[128];
            _networkStream.Read(lenBuffer, 0, lenBuffer.Length);
            var dataSize = int.Parse(Encoding.UTF8.GetString(lenBuffer));

            //响应服务端,区分两次发送的数据
            _networkStream.WriteByte(1);

            //接收服务端发送的数据
            var dataBuffer = new byte[dataSize];
            _networkStream.Read(dataBuffer, 0, dataBuffer.Length);
            var str = Encoding.UTF8.GetString(dataBuffer);
            return str;
        }

        /// <summary>
        /// 取消上一步的处理
        /// </summary>
        public void CancelLastTask()
        {
            var bytes = Encoding.UTF8.GetBytes("stop");
            //通知服务端终止上一个线程
            _networkStream.Write(bytes, 0, bytes.Length);
        }


        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            var bytes = Encoding.UTF8.GetBytes("close");
            //通知服务端关闭连接
            _networkStream.Write(bytes, 0, bytes.Length);
            _client?.Close();
        }

        /// <summary>
        /// 等待服务端响应，用于同步
        /// </summary>
        /// <returns></returns>
        private string GetResponse()
        {
            var recvBuffer = new byte[1];
            _networkStream.Read(recvBuffer, 0, recvBuffer.Length);
            var response = Encoding.UTF8.GetString(recvBuffer);
            return response;
        }

        public void Exit()
        {
            var bytes = Encoding.UTF8.GetBytes("exit");
            //通知服务端关闭连接
            _networkStream.Write(bytes, 0, bytes.Length);
            // _client?.Close();
        }

        /// <summary>
        /// 创建实例并连接服务端，别忘了close
        /// </summary>
        /// <param name="port">python服务端端口</param>
        public DpClient(int port = 33333)
        {
            Port = port;
            _client = new TcpClient(_ip, Port);
            _networkStream = _client.GetStream();
        }
    }
}