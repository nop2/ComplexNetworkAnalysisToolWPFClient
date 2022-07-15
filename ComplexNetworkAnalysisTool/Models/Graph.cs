using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using HandyControl.Controls;
using Newtonsoft.Json;
using RDotNet;

namespace ComplexNetworkAnalysisTool.Models
{
    public class Graph : ViewModelBase
    {
        private int _vertexCount;
        private int _edgeCount;
        private double _avgDegree;
        private int _maxDegree;
        private int _diameter;
        private double _avgPathLen;
        private double _clusteringCoefficient;
        private double _density;
        private int _cliqueCount;
        private string _isDirected = "无向图";

        /// <summary>
        /// VertexCount
        /// </summary>
        public string IsDirected
        {
            get => _isDirected;
            set { _isDirected = value; RaisePropertyChanged();}
        }

        public int VertexCount
        {
            get => _vertexCount;
            set
            {
                _vertexCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// EdgeCount
        /// </summary>
        public int EdgeCount
        {
            get => _edgeCount;
            set
            {
                _edgeCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// AvgDegree
        /// </summary>
        public double AvgDegree
        {
            get => _avgDegree;
            set
            {
                _avgDegree = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// MaxDegree
        /// </summary>
        public int MaxDegree
        {
            get => _maxDegree;
            set
            {
                _maxDegree = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Diameter
        /// </summary>
        public int Diameter
        {
            get => _diameter;
            set
            {
                _diameter = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// AvgPathLen
        /// </summary>
        public double AvgPathLen
        {
            get => _avgPathLen;
            set
            {
                _avgPathLen = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// ClusteringCoefficient
        /// </summary>
        public double ClusteringCoefficient
        {
            get => _clusteringCoefficient;
            set
            {
                _clusteringCoefficient = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Density
        /// </summary>
        public double Density
        {
            get => _density;
            set
            {
                _density = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// CliqueCount
        /// </summary>
        public int CliqueCount
        {
            get => _cliqueCount;
            set
            {
                _cliqueCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// DegreeList
        /// </summary>
        // public List<int> DegreeList { get; set; }
        //
        // /// <summary>
        // /// BetweennessList
        // /// </summary>
        // public List<double> BetweennessList { get; set; }
        //
        // /// <summary>
        // /// ClosenessList
        // /// </summary>
        // public List<double> ClosenessList { get; set; }

        // public List<double> PageRank { get; set; }

        public Graph()
        {
            //反序列化JsonConvert.DeserializeObject<Student>(json1)
            //Attributes = new NetworkAttributes();
        }
    }

}