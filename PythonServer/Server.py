import ctypes
import inspect
from time import sleep
import pandas as pd
import socket
import threading
import json
import pandas_profiling
from pandasql import sqldf
from sklearn.preprocessing import StandardScaler, MinMaxScaler, LabelEncoder
from scipy.spatial.distance import cdist
from MyNetwork import MyNetwork


class DataHelper:

    # 读取json格式配置文件
    def __init__(self, ini: dict, data: pd.DataFrame):
        self.ini = ini
        self.data = data.copy()

    # # 读取数据
    # def read_data(self):
    #     if self.ini['path'].endswith('.csv'):
    #         self.data = pd.read_csv(self.ini['path'])
    #     else:
    #         self.data = pd.read_excel(self.ini["path"])

    # 去除重复行
    def drop_duplicates(self):
        if self.data.empty:
            return
        if self.ini['drop_duplicates'] == 1:
            self.data.drop_duplicates(inplace=True)

    # 处理缺失值
    def fill_missing_data(self):
        if self.data.empty:
            return
        cmd = self.ini['fillna']
        # 不做处理
        if cmd == 0:
            return
        # 删除有缺失行
        elif cmd == 1:
            self.data.dropna(thresh=self.ini['fillna_thresh'], inplace=True)
        # 列均值填补
        elif cmd == 2:
            self.data.fillna(self.data.mean(), inplace=True)
        # 列中位数填补
        elif cmd == 3:
            self.data.fillna(self.data.median(), inplace=True)
        # 临近值填补
        elif cmd == 4:
            self.data.fillna(method='pad', inplace=True)

    # 处理异常值
    def process_abnormal_data(self):
        if self.data.empty:
            return
        cmd = self.ini['process_abnormal_data']
        if cmd == 0:
            return
        # 3-sigema 法则去除异常记录
        elif cmd == 1:
            for col in self.ini['process_abnormal_data_columns']:
                self.data = self.data[abs((self.data[col] - self.data[col].mean()) / self.data[col].std()) <= 3]

    # 数据标准化
    def standardized_data(self):
        if self.data.empty:
            return
        cmd = self.ini['standardized_data']
        if cmd == 0:
            return
        # 0-1标准化
        elif cmd == 1:
            self.data[self.ini['standardized_data_columns']] = MinMaxScaler().fit_transform(
                self.data[self.ini['standardized_data_columns']])
        # 标准差标准化
        elif cmd == 2:
            self.data[self.ini['standardized_data_columns']] = StandardScaler().fit_transform(
                self.data[self.ini['standardized_data_columns']])

    # 独热编码
    def one_hot_encoding(self):
        if self.data.empty:
            return
        if self.ini['one_hot_encoding'] == 1:
            self.data = pd.get_dummies(self.data, columns=self.ini['one_hot_encoding_columns'])

    # 文本向量化
    def label_encoding(self):
        if self.data.empty:
            return
        if self.ini['label_encoding'] == 1:
            le = LabelEncoder()
            for col in self.ini['label_encoding_columns']:
                self.data[col] = le.fit_transform(self.data[col])

    # 日期处理
    def date_process(self):
        if self.data.empty:
            return
        if self.ini['date_process'] == 0:
            return
        else:
            date = pd.to_datetime(self.ini['date'])
            for col in self.ini['date_columns']:
                self.data[col] = date - pd.to_datetime(self.data[col].astype('str'))
                # 到date的天数
                if self.ini['date_process'] == 1:
                    self.data[col] = pd.to_timedelta(self.data[col]).dt.days
                # 到date的秒数
                elif self.ini["date_process"] == 2:
                    self.data[col] = pd.to_timedelta(self.data[col]).dt.seconds

    # # 保存处理好的文件
    # def save_file(self):
    #     self.data.columns = [f'{col}${self.data[col].dtype}' for col in self.data.columns]
    #     self.data.to_csv(self.ini['save_path'], index=False)

    # 处理
    def process(self):
        # self.read_data()  # 读数据
        # print(self.data.head())
        self.drop_duplicates()  # 去除重复行
        # print(self.data.head())
        self.process_abnormal_data()  # 处理异常值
        # print(self.data.head())
        self.one_hot_encoding()  # 独热编码
        # print(self.data.head())
        # self.label_encoding()  # 文本向量化
        # print(self.data.head())
        # self.date_process()  # 处理日期
        # print(self.data.head())
        self.fill_missing_data()  # 填补空缺值
        # print(self.data.head())
        self.standardized_data()  # 标准化
        # print(self.data.head())
        # self.save_file()  # 保存文件
        return self.data


class Sever:
    def __init__(self, port=33333):
        self.is_exit = False  # 指示是否退出程序

        self.port = port
        self.clint_port = port + 1
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM, 0)  # 服务端sock
        self.sock.bind(('127.0.0.1', self.port))

        self.data: pd.DataFrame = pd.DataFrame()  # 表格数据
        self.processed_data: pd.DataFrame = pd.DataFrame()  # 数据预处理后的数据
        self.processed_data_filter: pd.DataFrame = pd.DataFrame()  # 从预处理数据中筛选的数据，用于计算相似度和生成节点表
        self.distance_data: pd.DataFrame = pd.DataFrame()  # 相似度数据
        self.distance_data_filter: pd.DataFrame = pd.DataFrame()  # 从相似度数据中筛选的数据，用于作为网络的边数据
        self.vertex_data: pd.DataFrame = None  # 从数据表格中筛选出的数据，作为网络的节点
        self.edge_data = None

        self.last_connect: socket.socket = None  # 上一个操作的连接
        self.last_thread: threading.Thread = None  # 上一个执行的线程
        self.last_path: str = ''  # 上一个读取的文件路径
        self.id_col_name = '-0ID0-'  # 添加的id列

        self.net: MyNetwork = MyNetwork()  # 网络对象

    def start(self) -> None:
        '''
        启动服务端接收客户端请求
        :return:
        '''

        self.sock.listen(100)
        while True:
            if self.is_exit:
                exit(0)
            connect, addr = self.sock.accept()
            print(addr)
            # 开启线程处理请求
            self.last_thread = threading.Thread(target=self.connect_handle, args=(connect,))
            self.last_thread.setDaemon(True)
            self.last_thread.start()

    def dataframe_map(self, df: str) -> pd.DataFrame:
        if df == 'data':
            return self.data
        elif df == "processed_data":
            return self.processed_data
        elif df == "processed_data_filter":
            return self.processed_data_filter
        elif df == "distance_data":
            return self.distance_data
        elif df == "distance_data_filter":
            return self.distance_data_filter
        elif df == "vertex_data":
            self.vertex_data = self.net.g.get_vertex_dataframe().reset_index()
            self.vertex_data.columns = [c.strip().replace(' ', '-').replace('_', '-') for c in self.vertex_data.columns]
            return self.vertex_data
        elif df == "edge_data":
            self.edge_data = self.net.g.get_edge_dataframe()
            self.edge_data.columns = [c.strip().replace(' ', '-').replace('_', '-') for c in self.edge_data.columns]
            return self.edge_data

    def connect_handle(self, connect: socket.socket):
        # 接收客户端请求数据
        try:
            while True:
                recv_cmd = connect.recv(4096).decode("utf-8").strip()
                print(recv_cmd)
                if "exit" in recv_cmd:
                    self.is_exit = True
                    exit(0)
                if recv_cmd == "close":
                    connect.close()
                    print('连接关闭')
                    return
                elif recv_cmd == "stop":
                    connect.close()
                    print('强制结束线程')
                    if self.last_connect is not None:
                        self.last_connect.close()

                    if self.last_thread is not None and self.last_thread.is_alive():
                        self._async_raise(self.last_thread.ident, SystemExit)
                    return

                recv_cmd = json.loads(recv_cmd)

                # 解析命令
                # 读取数据文件
                if recv_cmd['cmd'] == 'LoadData':
                    # cmd = {"cmd": "LoadData",
                    #      "len": "0",
                    #      "params": "C:a.html"}
                    path, encoding = recv_cmd['params'].split('?')
                    self.load_data(path, encoding.lower())
                    connect.sendall("1".encode("utf-8"))
                # 预览数据
                elif recv_cmd["cmd"] == "PeekData":
                    # cmd = {"cmd": "PeekData",
                    #      "len": "0",
                    #      "params": "133,data"}
                    # 获得要发送数据
                    params = recv_cmd['params'].split('?')
                    hasType = int(params[0])
                    max_line = int(params[1])
                    df = self.dataframe_map(params[2])

                    send_data = self.peek_data(df, max_line, hasType).encode('utf-8')
                    # 通告发送字节数，等待客户端响应（为了区分两次发送的数据）
                    connect.sendall(str(len(send_data)).encode("utf-8"))
                    connect.recv(8)
                    # 发送数据
                    connect.sendall(send_data)
                # 生成数据分析报告
                elif recv_cmd["cmd"] == "GetReport":
                    # cmd = {"cmd": "GetReport",
                    #      "len": "0",
                    #      "params": "C:a.html"}
                    pandas_profiling.ProfileReport(df=self.data.drop(columns=[self.id_col_name]),
                                                   minimal=True).to_file(recv_cmd['params'])
                    connect.sendall("1".encode("utf-8"))
                # 筛选数据
                elif recv_cmd["cmd"] == "FilteringData":
                    # cmd = {"cmd": "FilteringData",
                    #      "len": "0",
                    #      "params": "select * from data where a>10;"}
                    env = {
                        'data': self.data,
                        'processed_data': self.processed_data,
                        'processed_data_filter': self.processed_data_filter,
                        'distance_data': self.distance_data,
                        'distance_data_filter': self.distance_data_filter
                    }
                    if 'processed_data' in recv_cmd['params']:
                        # 对预处理后的数据进行筛选
                        self.processed_data_filter = sqldf(recv_cmd['params'], env)

                    elif 'distance_data' in recv_cmd["params"]:
                        # 对距离数据表进行筛选，同时初始化网络对象
                        if 'where' in recv_cmd["params"]:
                            self.distance_data_filter = sqldf(recv_cmd["params"], env)
                        else:
                            self.distance_data_filter = self.distance_data
                        # 初始化网络对象
                        self.vertex_data = self.get_vertex_df()
                        # self.vertex_data.to_csv('vertex_t.csv',index=False)
                        # self.distance_data_filter.to_csv('edge_t.csv',index=False)
                        # print('---------------------网络构建：数据表已导出-------------')
                        self.net.generate_graph_from_dataframe(e=self.distance_data_filter, v=self.vertex_data, r=None,
                                                               directed=False)

                        print('初始化网络完成')
                    connect.sendall("1".encode("utf-8"))
                    # self.processed_data_filter.to_csv('test.csv')

                # 数据预处理
                elif recv_cmd["cmd"] == "PreProcessData":
                    # cmd = {"cmd": "PreProcessData",
                    #      "len": "0",
                    #      "params": {"op":["1","d"]}}
                    dh = DataHelper(recv_cmd['params'], self.data)
                    self.processed_data = dh.process()
                    connect.sendall("1".encode("utf-8"))

                # 计算距离、相似度
                elif recv_cmd["cmd"] == "CalculateSimilarity":
                    # cmd = {"cmd": "CalculateSimilarity",
                    #      "len": "0",
                    #      "params": "euclidean"
                    # https://docs.scipy.org/doc/scipy/reference/generated/scipy.spatial.distance.cdist.html#scipy.spatial.distance.cdist
                    self.distance_data = self.get_distance_df(recv_cmd['params'])
                    print('------------相似度计算--------------')
                    print(self.distance_data.head())
                    connect.sendall("1".encode("utf-8"))

                # 数据表导出csv文件
                elif recv_cmd["cmd"] == "ExportCsv":
                    path, df = recv_cmd['params'].split('?')
                    df = self.dataframe_map(df)
                    df.to_csv(path, index=False)
                    connect.sendall("1".encode("utf-8"))

                # 生成交互式网络图
                elif recv_cmd["cmd"] == "GenerateNetwork":
                    # params = json.loads(recv_cmd["params"])
                    # print(params)
                    p = recv_cmd["params"]
                    print(p)
                    while not self.net.ready:
                        sleep(0.1)
                    self.net.export_network(**p)
                    connect.sendall("1".encode("utf-8"))
                # 获取网络描述信息
                elif recv_cmd["cmd"] == "GetGraphInfo":
                    msg = self.net.get_network_properties_json()
                    data = msg.encode("utf-8")
                    # 通告数据字节数,等待客户端响应
                    connect.sendall(str(len(data)).encode("utf-8"))
                    connect.recv(8)
                    # 发送数据
                    connect.sendall(data)
                # 社区发现
                elif recv_cmd["cmd"] == "CommunityDetection":
                    path, algorithm = recv_cmd["params"].split('?')
                    count = self.net.community_detect(path, algorithm)
                    connect.sendall(str(count).encode("utf-8"))

                # 度分布
                elif recv_cmd["cmd"] == "ShowDegreeDistribution":
                    path = recv_cmd["params"]
                    self.net.show_degree_distribution(path)
                    connect.sendall("1".encode("utf-8"))

                # 手动导入边数据和节点数据
                elif recv_cmd["cmd"] == "ImportData":
                    edge_path, vertex_path, relate_path, is_directed, is_overlay = recv_cmd["params"].split("?")
                    is_directed = is_directed.strip() == "1"
                    is_overlay = is_overlay.strip() == "1"

                    edge_data = None
                    vertex_data = None

                    edge_path = edge_path.lower()
                    if edge_path.endswith('.csv') or edge_path.endswith('.txt'):
                        edge_data = pd.read_csv(edge_path)
                    elif edge_path.endswith('.xls') or edge_path.endswith('.xlsx'):
                        edge_data = pd.read_excel(edge_path)

                    if vertex_path == '':
                        vertex_data = None
                    else:
                        vertex_path = vertex_path.lower()
                        if vertex_path.endswith('.csv') or vertex_path.endswith('.txt'):
                            vertex_data = pd.read_csv(vertex_path)
                        elif vertex_path.endswith('.xls') or vertex_path.endswith('.xlsx'):
                            vertex_data = pd.read_excel(vertex_path)

                    if relate_path == '':
                        relate_data = None
                    else:
                        relate_path = relate_path.lower()
                        if relate_path.endswith('.csv') or relate_path.endswith('.txt'):
                            relate_data = pd.read_csv(relate_path)
                        elif relate_path.endswith('.xls') or relate_path.endswith('.xlsx'):
                            relate_data = pd.read_excel(relate_path)

                    self.net.generate_graph_from_dataframe(v=vertex_data, e=edge_data, r=relate_data, directed=is_directed,overlay=is_overlay)


                    # 发送数据表列名
                    size = -1
                    data = None
                    if vertex_data is not None:
                        data = '$'.join(vertex_data.columns).encode('utf-8')
                        size = len(data)


                    # print(1)
                    # 通告字节数
                    connect.sendall(str(size).encode("utf-8"))
                    connect.recv(1)
                    # 发送数据
                    if size != -1:
                        connect.sendall(data)
                        connect.recv(1)
                    # print(2)
                    connect.sendall('1'.encode("utf-8"))
                # 导出图文件为graphml
                elif recv_cmd["cmd"] == "ExportGraphML":
                    self.net.g.write_graphml(f=recv_cmd["params"])
                    connect.sendall("1".encode("utf-8"))
                # 获得最短路径
                elif recv_cmd["cmd"] == "GetShortestPath":
                    start_id, end_id, label = recv_cmd["params"].split('?')
                    data = self.net.shortest_path_1_to_1_str(int(start_id), int(end_id), label).encode("utf-8")
                    data_size = len(data)
                    # 通告字节数
                    connect.sendall(str(data_size).encode("utf-8"))
                    connect.recv(1)
                    # 发送数据
                    connect.sendall(data)

        except Exception as e:
            connect.sendall("0".encode("utf-8"))
            # 连接C#端服务器，通知错误消息
            self.send_exception(str(e))

    def get_distance_df(self, metric: str = 'euclidean') -> pd.DataFrame:
        x = self.processed_data_filter.drop(columns=[self.id_col_name]).values
        x_dist = cdist(x, x, metric)

        ids = list(self.processed_data_filter[self.id_col_name])
        count = len(ids)
        items = []
        for i in range(count):
            for j in range(i + 1, count):
                items.append((ids[i], ids[j], x_dist[i][j]))
        a = pd.DataFrame(items, columns=['source', 'target', 'dist'])
        # a['dist_r'] = a['dist'].apply(lambda d: 1.0 / (1.0 + d))
        return a

    def get_vertex_df(self):
        return self.data.iloc[self.processed_data_filter[self.id_col_name], :]

    def load_data(self, path: str, encoding: str = "utf-8") -> None:
        lower_path = path.lower()
        if lower_path.endswith('.csv') or lower_path.endswith('.txt'):
            self.data = pd.read_csv(path, encoding=encoding)
        elif lower_path.endswith('.xls') or lower_path.endswith('.xlsx'):
            self.data = pd.read_excel(path)
        # 添加一列ID列 &ID%
        col_name = [self.id_col_name] + list(self.data.columns)
        self.data = self.data.reindex(columns=col_name)
        self.data[self.id_col_name] = self.data.index
        # 列名空格换成_
        # print(self.data.head())
        self.data.columns = [c.strip().replace(' ', '-').replace('_', '-') for c in self.data.columns]

    def peek_data(self, data: pd.DataFrame, lines: int = -1, has_type: int = 0) -> str:
        '''
        从DataFrame表中预览指定行数数据
        :param has_type: 列名是否包含列类型信息
        :param data: 数据表
        :param lines: 最大行数
        :return: csv格式字符串
        '''

        columns = data.columns.tolist()
        type_columns = [f'{col}${data[col].dtype}' if col != self.id_col_name else col for col in columns]

        if has_type == 1:
            data.columns = type_columns

        if lines == -1:
            data_temp = data
        else:
            data_temp = data[:lines]

        if self.id_col_name in columns:
            data_temp = data_temp.drop(columns=[self.id_col_name])

        csv_str = data_temp.to_csv(index=False)
        data.columns = columns
        return csv_str

    def _async_raise(self, tid, exctype):
        """raises the exception, performs cleanup if needed"""
        tid = ctypes.c_long(tid)
        if not inspect.isclass(exctype):
            exctype = type(exctype)
        res = ctypes.pythonapi.PyThreadState_SetAsyncExc(tid, ctypes.py_object(exctype))
        if res == 0:
            raise ValueError("invalid thread id")
        elif res != 1:
            # """if it returns a number greater than one, you're in trouble,
            # and you should call it again with exc=NULL to revert the effect"""
            ctypes.pythonapi.PyThreadState_SetAsyncExc(tid, None)
            raise SystemError("PyThreadState_SetAsyncExc failed")

    def send_exception(self, exception_info: str):
        '''
        向C#客户端发送异常信息
        :param exception_info: 异常信息
        :return:
        '''
        client = socket.socket(socket.AF_INET, socket.SOCK_STREAM, 0)  # 用于报告错误的客户端，只发不收
        client.connect(('127.0.0.1', self.clint_port))
        client.sendall(exception_info.encode("utf-8"))
        client.close()


if __name__ == '__main__':
    sever = Sever()
    print('服务端已启动：')
    sever.start()
