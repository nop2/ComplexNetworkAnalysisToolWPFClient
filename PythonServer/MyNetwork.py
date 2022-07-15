import math
import random
import threading

import faker
import igraph as ig
import pandas as pd
import numpy as np
from pyvis.network import Network
import json


# 网络类，用于构造网络，计算网络度量值，生成交互式网络，进行网络社团分析以及网络结构可视化
class MyNetwork:
    def __init__(self):
        self.lock = threading.RLock()

        self.html_template = ''  # 网页可视化模板
        self.bar_template = ''  # 柱状图模板
        with open("template.html", encoding="utf-8") as fp:
            self.html_template = fp.read()

        with open("bar_template.html", encoding="utf-8") as fp:
            self.bar_template = fp.read()

        self.g: ig.Graph = None  # 网络对象
        self.gs = []  # 网络图列表，有多个网络表示叠加
        self.vg: Network = None  # 动态可视化网络对象
        # self.vgs = []  # 动态可视化网络对象 列表
        self.relate = []  # 多个网络的边关系数据表文件 relate[1]表示网络2和网络1、网络0之间的关系
        self.id_map = []  # 网络节点id映射列表
        self.start_node_id = 0  # 节点映射起始

        self.vertex_df: pd.DataFrame = None  # 节点文件表

        # 最近一个网络的度、度分布、聚集系数、介数、中心性、度相关性
        self.vertex_count = None  # 节点数
        self.edge_count = None  # 边数
        self.avg_degree = None  # 平均度
        self.max_degree = None  # 最大度
        self.degree_list = None  # 节点度列表
        self.diameter = None  # 网络直径
        self.avg_path_len = None  # 平均路经长
        self.clustering_coefficient = None  # 聚集系数
        self.density = None  # 网络密度
        self.betweenness = None  # 介数中心性列表
        self.closeness = None  # 紧密中心性列表
        self.clique_number = None  # 团数量
        self.page_rank = None  # pagerank值
        self.colors = ['#70f3ff', '#44cef6', '#3eede7', '#1685a9', '#177cb0', '#065279', '#003472', '#4b5cc4',
                       '#a1afc9', '#2e4e7e',
                       '#3b2e7e', '#4a4266', '#426666', '#425066', '#574266', '#8d4bbb', '#815463', '#815476',
                       '#4c221b', '#003371',
                       '#56004f', '#801dae', '#4c8dae', '#b0a4e3', '#cca4e3', '#edd1d8', '#e4c6d0', '#ff461f',
                       '#ff2d51', '#f36838',
                       '#ed5736', '#ff4777', '#f00056', '#ffb3a7', '#f47983', '#db5a6b', '#c93756', '#f9906f',
                       '#f05654', '#ff2121',
                       '#f20c00', '#8c4356', '#c83c23', '#9d2933', '#ff4c00', '#ff4e20', '#f35336', '#dc3023',
                       '#ff3300', '#cb3a56',
                       '#a98175', '#b36d61', '#ef7a82', '#ff0097', '#c32136', '#be002f', '#c91f37', '#bf242a',
                       '#c3272b', '#9d2933',
                       '#60281e', '#622a1d', '#bce672', '#c9dd22', '#bddd22', '#afdd22', '#a3d900', '#9ed900',
                       '#9ed048', '#96ce54',
                       '#00bc12', '#0eb83a', '#0eb83a', '#0aa344', '#16a951', '#21a675', '#057748', '#0c8918',
                       '#00e500', '#40de5a',
                       '#00e079', '#00e09e', '#3de1ad', '#2add9c', '#2edfa3', '#7fecad', '#a4e2c6', '#7bcfa6',
                       '#1bd1a5', '#48c0a3',
                       '#549688', '#789262', '#758a99', '#50616d', '#424c50', '#41555d', '#eaff56', '#fff143',
                       '#faff72', '#ffa631',
                       '#ffa400', '#fa8c35', '#ff8c31', '#ff8936', '#ff7500', '#ffb61e', '#ffc773', '#ffc64b',
                       '#f2be45', '#f0c239',
                       '#e9bb1d', '#d9b611', '#eacd76', '#eedeb0', '#d3b17d', '#e29c45', '#a78e44', '#c89b40',
                       '#ae7000', '#ca6924',
                       '#b25d25', '#b35c44', '#9b4400', '#9c5333', '#a88462', '#896c39', '#827100', '#6e511e',
                       '#7c4b00', '#955539',
                       '#845a33', '#ffffff', '#e9e7ef', '#f0f0f4', '#e9f1f6', '#f0fcff', '#e3f9fd', '#d6ecf0',
                       '#fffbf0', '#f2ecde',
                       '#fcefe8', '#fff2df', '#f3f9f1', '#e0eee8', '#e0f0e9', '#c0ebd7', '#bbcdc5', '#c2ccd0',
                       '#bacac6', '#808080',
                       '#75878a', '#88ada6', '#6b6882', '#725e82', '#3d3b4f', '#392f41', '#75664d', '#5d513c',
                       '#665757', '#493131',
                       '#312520', '#161823']
        self.community_detect_algorithm_dict = None
        self.ready = False  # 网络是否初始化完成

    def get_network_properties_json(self) -> str:
        '''
        网络属性组成的json格式数据
        :return:
        '''
        directed = "有向图" if self.g.is_directed() else "无向图"
        data = f'{{' \
               f'"IsDirected":\"{directed}\",' \
               f'"VertexCount":{self.vertex_count},' \
               f'"EdgeCount":{self.edge_count},' \
               f'"AvgDegree":{self.avg_degree},' \
               f'"MaxDegree":{self.max_degree},' \
               f'"Diameter":{self.diameter},' \
               f'"AvgPathLen":{self.avg_path_len},' \
               f'"ClusteringCoefficient":{self.clustering_coefficient},' \
               f'"Density":{self.density},' \
               f'"CliqueCount":{self.clique_number},' \
               f'}}'
        # f'"DegreeList":{self.degree_list},' \
        # f'"BetweennessList":{self.betweenness},' \
        # f'"ClosenessList":{self.closeness}' \

        return data

    def generate_graph_from_dataframe(self, v: pd.DataFrame, e: pd.DataFrame, r: pd.DataFrame, directed=False,
                                      overlay=False) -> None:
        '''
        从dataframe中初始化网络，并获取网络各类属性值
        :param v: 节点表
        :param e: 边表
        :param directed: 是否是有向图
        '''

        # 加锁
        self.lock.acquire()
        try:
            # self.vertex_df = v
            # 生成网络图对象
            self.ready = False

            self.g = ig.Graph.DataFrame(edges=e, vertices=v, directed=directed)  # igraph图对象

            # 获取网络属性
            self.vertex_count = self.g.vcount()  # 节点数
            self.edge_count = self.g.ecount()  # 边数
            self.avg_degree = round(np.average(self.g.degree()), 4)  # 平均度
            self.max_degree = self.g.maxdegree()  # 最大度
            self.degree_list = self.g.degree()  # 节点度列表
            self.diameter = self.g.diameter()  # 网络直径
            self.avg_path_len = round(self.g.average_path_length(), 4)  # 平均路经长
            self.clustering_coefficient = round(self.g.transitivity_undirected(), 4)  # 聚集系数
            self.density = round(self.g.density(), 4)  # 网络密度
            self.clique_number = self.g.clique_number()  # 团数量

            self.g.vs.set_attribute_values(attrname='_度_', values=self.degree_list)

            # self.betweenness = self.g.betweenness()  # 介数列表
            self.betweenness = [-1 if np.isnan(v) else round(v, 5) for v in self.g.betweenness()]
            self.g.vs.set_attribute_values(attrname='_介数_', values=self.betweenness)
            # self.closeness = self.g.closeness()  # 紧密中心性列表
            self.closeness = [-1 if np.isnan(v) else round(v, 5) for v in self.g.closeness()]
            self.g.vs.set_attribute_values(attrname='_紧密中心性_', values=self.closeness)
            self.page_rank = [-1 if np.isnan(v) else round(v, 5) for v in self.g.pagerank()]
            self.g.vs.set_attribute_values(attrname='_PageRank_', values=self.page_rank)

            self.ready = True
            self.community_detect_algorithm_dict = {
                'EdgeBetweenness': self.g.community_edge_betweenness,  # 速度慢
                'FastGreedy': self.g.community_fastgreedy,  # 不能有重复边
                'InfoMap': self.g.community_infomap,  # 返回VertexClustering
                'LabelPropagation': self.g.community_label_propagation,
                'LeadingEigenvector': self.g.community_leading_eigenvector,  # 速度较慢
                # 'LeadingEigenvectorNaive': self.g.community_leading_eigenvector_naive,
                'Leiden': self.g.community_leiden,
                'MultiLevel': self.g.community_multilevel,
                'SpinGlass': self.g.community_spinglass,  # 速度慢
                'WalkTrap': self.g.community_walktrap
            }

            if not overlay:
                self.gs.clear()
                self.relate.clear()
                self.id_map.clear()
                self.start_node_id = 0

            self.gs.append(self.g.copy())
            self.relate.append(r)

            # 不使用网络叠加则新创建vg图对象
            if not overlay:
                self.vg = Network(height='100%', width='100%', directed=directed)  # 可视化图对象
                self.vg.add_nodes([i for i in range(self.g.vcount())])
                for i in range(self.g.vcount()):
                    self.vg.nodes[i]['graphId'] = len(self.gs) - 1
                self.vg.add_edges(self.g.get_edgelist())
                self.vg.show_buttons(filter_=['physics'])
            else:
                # 使用网络叠加则新增节点和边到现有vg对象，节点编号顺次增加
                self.vg.add_nodes([self.start_node_id + i for i in range(self.g.vcount())])
                for i in [self.start_node_id + i for i in range(self.g.vcount())]:
                    self.vg.nodes[i]['graphId'] = len(self.gs) - 1
                edge_list = self.g.get_edgelist()

                c = 0
                for p in edge_list:
                    edge_list[c] = [p[0] + self.start_node_id, p[1] + self.start_node_id]
                    c += 1
                self.vg.add_edges(edge_list)

            # 原先节点和修改后节点的映射
            if v is not None:
                self.id_map.append(
                    dict(zip(v[v.columns[0]], [self.start_node_id + i for i in range(self.vertex_count)])))
            else:
                self.id_map.append(dict(zip([i for i in range(self.vertex_count)],
                                            [self.start_node_id + i for i in range(self.vertex_count)])))

            self.start_node_id += self.vertex_count

        finally:
            self.lock.release()

    def export_network(self, path: str, isHtml: int = 1, vertex_size: str = '', vertex_color: str = '',
                       vertex_shape='circle',
                       vertex_label: str = '', edge_weight: str = '', layout: str = 'kk'
                       ):
        '''
        网络可视化
        :param path: 保存路径
        :param isHtml: 是否生成HTML文件或是svg图片
        :param vertex_size: 节点大小       _默认_、_度_、_介数_、_紧密中心性_、其他数值属性
        :param vertex_color: 节点颜色      _默认_#789262、_随机_、_度_、_介数_、_紧密中心性_、其他属性
        :param vertex_label: 节点标签，默认为节点ID
        :param edge_weight: 边宽度         _默认_、其他数值属性
        :param layout: 静态图布局
        :param size: 静态图图像大小
        '''

        self.lock.acquire()

        try:

            # 如果进行网络叠加，先处理关系连边
            if len(self.gs) > 1:
                r: pd.DataFrame = self.relate[-1]
                have_weight = len(r.columns) == 3 and ('weight' in r.columns or 'dist' in r.columns)
                for t in r.itertuples():
                    source_id = -1
                    for m in self.id_map[:-1]:
                        # print(t[1]) # t[0]为index
                        if t[1] in m.keys():
                            source_id = m[t[1]]
                            break
                    if source_id != -1:
                        self.vg.add_edge(source_id, self.id_map[-1][t[2]], color='red', dashes='true',
                                         value=3 if not have_weight else t[3])

            if vertex_size != '':
                if vertex_size == '_默认_':
                    vertex_size = None
                elif vertex_size == "_度_":
                    vertex_size = [int(20 + d * 5) for d in self.value_map(self.degree_list, 1, 10)]
                elif vertex_size == "_介数_":
                    vertex_size = [int(20 + d * 5) for d in self.value_map(self.betweenness, 1, 10)]
                elif vertex_size == "_紧密中心性_":
                    vertex_size = [int(20 + d * 5) for d in self.value_map(self.closeness, 1, 10)]
                else:
                    if vertex_size in self.g.vs.attribute_names():

                        try:
                            x = np.array(self.g.vs.get_attribute_values(vertex_size))
                            vertex_size = [int(30 + d * 40) for d in self.value_map(x, 1, 10)]
                        except Exception as e:
                            vertex_size = None
            else:
                vertex_size = None

            if vertex_color != '':
                if vertex_color.startswith('#'):
                    # vertex_color = [f"#{vertex_color.split('#')[-1]}"] * self.vertex_count
                    # vertex_color = [random.choice(self.colors)] * self.vertex_count
                    vertex_color = [vertex_color] * self.vertex_count
                elif vertex_color == "_随机_":
                    colors = random.sample(self.colors, 20)
                    vertex_color = [random.choice(colors) for _ in range(self.vertex_count)]
                elif vertex_color == "_度_":
                    vertex_color = self.value2color(self.degree_list, 10)
                elif vertex_color == "_介数_":
                    vertex_color = self.value2color(self.betweenness, 10)
                elif vertex_color == "_紧密中心性_":
                    vertex_color = self.value2color(self.closeness, 10)
                else:
                    if vertex_color in self.g.vs.attribute_names():
                        attr = self.g.vs.get_attribute_values(vertex_color)

                        try:
                            x = np.array(attr)
                            vertex_color = self.value2color(x, 10)
                        except Exception as e:
                            vertex_color = self.category2color(attr)
            else:
                vertex_color = [random.choice(self.colors)] * self.vertex_count

            vertex_image = None
            if 'image' in vertex_shape:
                if 'image' in self.g.vs.attribute_names():
                    vertex_image = [str(i) for i in self.g.vs.get_attribute_values('image')]

            if vertex_label == "_默认_":
                if 'name' in self.g.vs.attribute_names():
                    vertex_label = [str(i) for i in self.g.vs.get_attribute_values('name')]
                else:
                    vertex_label = [str(i) for i in range(self.vertex_count)]
            else:
                if vertex_label in self.g.vs.attribute_names():
                    vertex_label = [str(i) for i in self.g.vs.get_attribute_values(vertex_label)]
                else:
                    vertex_label = [str(i) for i in range(self.vertex_count)]

            if edge_weight in self.g.es.attribute_names():
                try:
                    if edge_weight == 'dist':
                        edge_weight = [int(10 + i * 20) for i in
                                       1.0 / np.array(self.value_map(self.g.es.get_attribute_values('dist'), 0.2, 1))]
                    elif edge_weight == "weight":
                        edge_weight = [int(10 + i * 20) for i in
                                       np.array(self.value_map(self.g.es.get_attribute_values('weight'), 0.2, 1))]
                    else:
                        edge_weight = None
                except Exception as e:
                    edge_weight = None
            else:
                edge_weight = None

            if isHtml == 1:
                nodes = self.vg.nodes  # 全部的节点
                offset = self.start_node_id - self.vertex_count

                if len(self.gs) > 1:
                    # 最后一个网络的节点id范围
                    ids = [i for i in range(offset, self.start_node_id)]
                else:
                    ids = [i for i in range(self.vertex_count)]
                    offset = 0

                if vertex_size is not None:
                    for i in ids:
                        nodes[i]['value'] = vertex_size[i - offset]
                if vertex_color is not None:
                    for i in ids:
                        nodes[i]['color'] = vertex_color[i - offset]
                if vertex_label is not None:
                    for i in ids:
                        nodes[i]['label'] = vertex_label[i - offset]

                if edge_weight is not None:
                    i = 0
                    for edge in self.vg.edges:
                        edge['value'] = edge_weight[i - offset]
                        i += 1

                # 节点形状与标签
                attributes_name = self.g.vs.attribute_names()
                vertex_attributes = {name: self.g.vs.get_attribute_values(name) for name in attributes_name}
                for i in ids:
                    nodes[i]['shape'] = vertex_shape
                    # self.vg.edges[i]['color'] = 'red'
                    # self.vg.edges[i]['dashes'] = 'true'
                    # title = f"<br>ID:{i} 标签:{self.vg.nodes[i - offset]['label']} 度:{self.degree_list[i - offset]}<br>" \
                    #         f"<br>介数:{self.betweenness[i - offset]} 紧密中心度:{self.closeness[i - offset]}<br> <br>社团：{vertex_community[i - offset]}<br>"
                    title = f'ID:{i}\n'
                    for name in attributes_name:
                        title += f"{name}:{vertex_attributes[name][i - offset]}\n"

                    self.vg.nodes[i]['title'] = title

                if 'image' in vertex_shape:
                    for i in ids:
                        nodes[i]["image"] = vertex_image[i - offset]

                self.export_html(path=path, graph=self.vg, isOverlay=True)
                # self.vg.force_atlas_2based()
                # self.vg.show_buttons()
                # self.vg.write_html(path)
                # self.vg.show(path)
            else:
                if vertex_size is None:
                    vertex_size = 20
                if layout == '':
                    layout = 'circle'
                ig.plot(self.g, target=path, vertex_size=vertex_size, vertex_color=vertex_color,
                        vertex_label=vertex_label,
                        edge_width=edge_weight, layout=layout)
        finally:
            self.lock.release()

    def export_html(self, path: str, graph: Network, isCommunity: bool = False, isOverlay: bool = False):
        """
        由pyvis Network对象生成适合echarts的数据集
        :param graph:
        :return:
        """

        global categories
        nodes = []
        community_color_map = {}
        for v in graph.nodes:
            it = {
                "id": str(v['id']),  # id 字符串
                "name": str(v['label']),  # label
                "value": v['title'],  # 额外信息
                "symbol": v['shape'],  # 形状
                "symbolSize": v['value'],  # 节点大小
                "itemStyle": {  # 节点颜色
                    "color": v['color'],
                }}
            if isCommunity:
                try:
                    it['category'] = v['community']
                    community_color_map[v['community']] = v['color']
                except:
                    pass
            elif isOverlay:
                it['category'] = v['graphId']
            nodes.append(it)

        edges = []
        for e in graph.edges:
            it = {
                "source": str(e['from']),
                "target": str(e['to']),
            }
            if "dashes" in e.keys():
                it['lineStyle'] = {
                    "color": "red",
                    "width": 3,
                    "type": "dashed",
                }
            else:
                it['lineStyle'] = {
                    "color": graph.nodes[e["from"]]['color'],
                    "width": 1,
                    "type": "solid",
                }

            edges.append(it)

        categories = []
        if isCommunity:
            try:
                categories = [{
                    "name": f"社团{i + 1}",
                    "itemStyle": {
                        "color": community_color_map[i]
                    }
                } for i in range(len(community_color_map))]
            except:
                pass
        elif isOverlay:
            categories = [{
                "name": f"图{i + 1}",
            } for i in range(len(self.gs))]

        # 边上的箭头
        if isOverlay:
            d = self.gs[0].is_directed()
        else:
            d = self.g.is_directed()

        data = {
            "nodes": nodes,
            "edges": edges,
            "categories": categories,
            "directed": 1 if d else 0
        }

        html = self.html_template.replace('"$graph$"', str(data))
        with open(path, "w+", encoding="utf-8") as fp:
            fp.write(html)

    def community_detect(self, path: str, algorithm: str) -> int:
        '''
        执行社团发现算法，并生成网络图
        :param path:
        :param algorithm:
        :return:
        '''

        self.lock.acquire()

        try:
            community: [ig.VertexDendrogram, ig.VertexClustering] = self.community_detect_algorithm_dict[algorithm]()
            # count = community.optimal_count  # 社团数量

            if type(community) is ig.VertexClustering:
                vertex_community = community.membership
            else:
                vertex_community = community.as_clustering().membership  # 节点所在社团
            vertex_colors = self.category2color(vertex_community)

            # print(f'社团数:{max(vertex_community) + 1}')
            offset = self.start_node_id - self.vertex_count
            if len(self.gs) > 1:
                # 最后一个网络的节点id范围
                ids = [i for i in range(offset, self.start_node_id)]
            else:
                ids = [i for i in range(self.vertex_count)]
                offset = 0

            attributes_name = self.g.vs.attribute_names()
            vertex_attributes = {name: self.g.vs.get_attribute_values(name) for name in attributes_name}

            for i in ids:
                self.vg.nodes[i]['color'] = vertex_colors[i - offset]
                self.vg.nodes[i]['community'] = vertex_community[i - offset]

                # self.vg.nodes[i][
                #     'title'] = f"ID:{i} 标签:{self.vg.nodes[i - offset]['label']} 度:{self.degree_list[i - offset]}<br>" \
                #                f"介数:{self.betweenness[i - offset]} 紧密中心度:{self.closeness[i - offset]} <br>社团：{vertex_community[i - offset]}<br>"

                title = f'ID:{i}\n'
                for name in attributes_name:
                    title += f"{name}:{vertex_attributes[name][i - offset]}\n"

                self.vg.nodes[i]['title'] = title

            # self.vg.write_html(path)
            self.export_html(path=path, graph=self.vg, isCommunity=True)

            self.g.vs.set_attribute_values(attrname='_社团_', values=vertex_community)
            return max(vertex_community) + 1
        finally:
            self.lock.release()

    def show_degree_distribution(self, path: str):
        '''
        度分布图
        :return:
        '''
        degree = self.g.degree()
        m = {}
        for i in degree:
            if i in m.keys():
                m[i] += 1
            else:
                m[i] = 1

        l = [(k, v) for k, v in m.items()]
        l.sort(key=lambda k: k[0])
        x = [i[0] for i in l]
        y = [i[1] for i in l]

        html = self.bar_template.replace('"$data$"', str({"x": x, "y": y}))
        with open(path, "w+", encoding="utf-8") as fp:
            fp.write(html)

    def value_map(self, nums, target_min, target_max):
        '''
        将数值映射到另一个区间
        :param nums:
        :param target_min:
        :param target_max:
        :return:
        '''
        x = nums
        if type(nums) is not np.ndarray:
            x = np.array(nums)
        s_min = np.min(x)
        s_max = np.max(x)
        return target_min + (target_max - target_min) / (s_max - s_min) * (x - s_min)

    def value2color(self, nums, color_num):
        '''
        连续值映射为颜色
        :param nums: 值
        :param color_num: 映射的颜色数
        :return:
        '''
        x = self.value_map(nums, 1, color_num)
        colors = random.sample(self.colors, color_num + 1)
        return [colors[math.ceil(c)] for c in x]

    def category2color(self, category):
        '''
        离散值转颜色
        :param category: 离散数据
        :return:
        '''
        u_c = list(set(category))
        random.shuffle(self.colors)
        color_dict = {}

        i = 0
        color_len = len(self.colors)
        for c in u_c:
            color_dict[c] = self.colors[i]
            i += 1
            if i >= color_len:
                i = color_len - 1

        return [color_dict[c] for c in category]

    def shortest_path_1_to_1_str(self, start_node_id, end_node_id, label) -> str:
        if label == '' or label not in self.g.vs.attribute_names():
            label = 'name'
        path = self.g.get_shortest_paths(start_node_id, end_node_id)[0]
        df = pd.DataFrame(columns=['索引', 'ID', '标签'])
        df['索引'] = [i for i in range(len(path))]
        df['ID'] = path
        df['标签'] = df['ID'].apply(lambda x: self.g.vs[x][label])
        return df.to_csv(index=False)


if __name__ == "__main__":

    node1 = pd.read_excel(r'E:\文件\毕业设计\数据\公交站点.xlsx')
    edge1 = pd.read_excel(r'E:\文件\毕业设计\数据\公交边.xlsx')

    node2 = pd.read_excel(r'E:\文件\毕业设计\数据\地铁站点.xlsx')
    edge2 = pd.read_excel(r'E:\文件\毕业设计\数据\地铁边.xlsx')

    relation = pd.read_excel(r'E:\文件\毕业设计\数据\关系.xlsx')


    # e = pd.read_csv('测试_人物关系.csv')

    net = MyNetwork()
    p_kw = {
        "path": "a.html",  # 保存路径
        "isHtml": 1,  # 可交互网络图
        "vertex_size": "_度_",  # 节点大小设置
        "vertex_color": "_随机_",  # 节点颜色设置
        "vertex_shape": "circle",
        # 'circle', 'rect', 'roundRect', 'triangle', 'diamond', 'pin', 'arrow', 'none','image://http://xxx.xxx.xxx/a/b.png'
        "vertex_label": "name",  # 节点标签设置
        "edge_weight": "weight",  # 边权重
        "layout": "lgl",  # 图片布局
        # "size": 1000,  # 图片大小
    }

    m_kw = {
        "path": "b.html",  # 保存路径
        "isHtml": 1,  # 可交互网络图
        "vertex_size": "_度_",  # 节点大小设置
        "vertex_color": "_随机_",  # 节点颜色设置
        "vertex_shape": "rect",
        "vertex_label": "amap_station_name",  # 节点标签设置
        "edge_weight": "dist",  # 边权重
        "layout": "lgl",  # 图片布局
        # "size": 1000,  # 图片大小
    }

    net.generate_graph_from_dataframe(v=node1, e=edge1, r=None, directed=True)
    net.export_network(**p_kw)

    net.generate_graph_from_dataframe(v=node2, e=edge2, r=relation, directed=True, overlay=True)
    net.export_network(**m_kw)

    print(net.vertex_count)
    print(net.g.vs[0])
    print(net.edge_count)
    print(net.g.vs.attribute_names())
    print(net.g.vs)

    net.show_degree_distribution("degree.html")
    print('导出网络图')

    # print(net.g.get_vertex_dataframe()[:100])

    # a = net.g.get_shortest_paths(100, 3)[0]
    # if len(a) == 0:
    #     print('没有最短路径')
    # print([f'{v}:{names[v]}' for v in a[0]])
    # df = pd.DataFrame(columns=['索引', 'ID', '标签'])
    # df['索引'] = [i for i in range(len(a))]
    # df['ID'] = a
    # df['标签'] = df['ID'].apply(lambda x: net.g.vs[x]['name'])
    # print(df.to_csv(index=False))
    # print(a)

    # print(net.g.is_directed())
    # print('导出完成')
    # print(net.get_network_properties_json())
    # print('社团发现')
    # net.community_detect('community_detect.html', 'SpinGlass') # 不能在非连通图使用
    # net.community_detect('community_detect.html', 'LabelPropagation')
    # # net.community_detect('community_detect.html', 'MultiLevel')
    # # net.community_detect('community_detect.html', 'EdgeBetweenness')
    # print('社团发现完成')
    #
    # print(net.g.k_core(1))
