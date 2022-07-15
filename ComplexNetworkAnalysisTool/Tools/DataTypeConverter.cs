using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComplexNetworkAnalysisTool.Tools
{
    public static class DataTypeConverter
    {
        public static Type GetTypeFromStr(this string str)
        {
            if (string.IsNullOrEmpty(str) || !str.Contains("$"))
            {
                return typeof(string);
            }

            var type = str.Split('$').Last().ToLower();

            if (type.Contains("uint8"))
                return typeof(int);
            else if (type.Contains("int"))
                return typeof(int);
            else if (type.Contains("float"))
                return typeof(double);
            else if (type.Contains("datetime"))
                return typeof(string);
            return typeof(string);
        }
    }
}
