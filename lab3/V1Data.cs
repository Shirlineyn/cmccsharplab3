using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Doronin_LAB_2
{

    public delegate DataItem FDI(double x);
    public delegate void FValues(double x, ref double y1, ref double y2);

    public struct DataItem
    {
        public double x { get; set; }
        public double y1 { get; set; }
        public double y2 { get; set; }
        public DataItem(double x = 0.0, double y1 = 0.0, double y2 = 0.0)
        {
            this.x = x;
            this.y1 = y1;
            this.y2 = y2;
        }
        public double Abs()
        {
            return Math.Sqrt(Math.Pow(y1, 2) + Math.Pow(y2, 2));
        }
        public string ToLongString(string format)
        {
            return string.Format(format, x, y1, y2);
        }
        public override string ToString()
        {
            return "X is " + x.ToString() + "; Y1 is " + y1.ToString() + "; Y2 is " + y2.ToString() + ".";
        }
    }
    public abstract class V1Data : IEnumerable<DataItem>
    {
        public string key { get; set; }
        public DateTime time { get; set; }
        public V1Data(string key, DateTime time)
        {
            this.key = key;
            this.time = time;
        }
        public abstract double MaxDistance { get; }
        public abstract string ToLongString(string format);
        public abstract IEnumerator<DataItem> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        { return (IEnumerator)GetEnumerator(); }
        public override string ToString()
        {
            return "Key: " + key.ToString() + "; Time: " + time.ToString() + ".";
        }
    }

    public class V1DataList : V1Data
    {
        public List<DataItem> data { get; set; }
        public V1DataList(string key, DateTime time) : base(key, time)
        {
            data = new List<DataItem>();
        }
        public V1DataList(string key, DateTime time, double[] x, FDI F) : base(key, time)
        {
            data = new List<DataItem>();
            foreach (double xelem in x)
            {
                if (!data.Exists(sample => sample.x == xelem))
                {
                    data.Add(F(xelem));
                }
            }
            data.Sort((x, y) => x.x.CompareTo(y.x));
        }
        public override double MaxDistance
        {
            get
            {
                if (data.Count == 0)
                    return 0;
                double min = data[0].x, max = min;
                foreach (DataItem item in data)
                {
                    if (item.x < min)
                    {
                        min = item.x;
                    }
                    else if (item.x > max)
                    {
                        max = item.x;
                    }
                }
                return max - min;
            }
        }
        public static explicit operator V1DataArray(V1DataList source)
        {
            source.data.Sort((x, y) => x.x.CompareTo(y.x));
            V1DataArray dataarray = new V1DataArray(source.key, source.time);
            double[] datax = new double[source.data.Count];
            double[][] datay = new double[2][];
            datay[0] = new double[source.data.Count];
            datay[1] = new double[source.data.Count];
            for (int i = 0; i < source.data.Count; i++)
            {
                datax[i] = source.data[i].x;
                datay[0][i] = source.data[i].y1;
                datay[1][i] = source.data[i].y2;
            }
            dataarray.datax = datax;
            dataarray.datay = datay;
            return dataarray;
        }
        public override string ToString()
        {
            return "Type: V1DataList; Elem count: " + data.Count().ToString() + " " + base.ToString();
        }
        public override string ToLongString(string format)
        {
            string result = this.ToString() + "\nList:\n";
            foreach (DataItem item in data)
            {
                result += "x: " + string.Format(format, item.x)
                    + "; y1: " + string.Format(format, item.y1)
                    + "; y2: " + string.Format(format, item.y2) + ";\n";
            }
            return result;
        }
        public override IEnumerator<DataItem> GetEnumerator()
        {
            return data.GetEnumerator();
        }
    }

    public class V1DataArray : V1Data
    {

        public double[] datax { get; set; }
        public double[][] datay { get; set; }
        public V1DataArray(string key, DateTime time) : base(key, time)
        {
            datax = new double[0];
            datay = new double[2][];
            datay[0] = new double[0];
            datay[1] = new double[0];
        }
        public V1DataArray(string key, DateTime time, double[] x, FValues F) : base(key, time)
        {
            datax = x.OrderBy(x => x).Distinct().ToArray();
            datay = new double[2][];
            datay[0] = new double[datax.Length];
            datay[1] = new double[datax.Length];
            for (int i = 0; i < datax.Length; i++)
            {
                F(datax[i], ref datay[0][i], ref datay[1][i]);
            }
        }
        public V1DataArray(string key, DateTime time, int nX, double xL, double xR, FValues F)
            : base(key, time)
        {
            double step = (xR - xL) / (nX + 1);
            datax = new double[nX + 2];
            datay = new double[2][];
            datay[0] = new double[nX + 2];
            datay[1] = new double[nX + 2];
            for (int i = 0; i < nX + 2; i++)
            {
                datax[i] = xL + step * i;
                F(datax[i], ref datay[0][i], ref datay[1][i]);
            }
        }
        public double[] this[int index]
        {
            get
            { // add exception
                return datay[index];
            }
        }
        public V1DataList GetV1DataList
        {
            get
            {
                List<DataItem> datalist = new List<DataItem>();
                for (int i = 0; i < datax.Length; i++)
                {
                    datalist.Add(new DataItem(datax[i], datay[0][i], datay[1][i]));
                }
                V1DataList newlist = new V1DataList(this.key, this.time);
                newlist.data = datalist;
                return newlist;
            }
        }
        public override double MaxDistance
        {
            get
            {
                if(datax.Length == 0)
                    return 0;
                return datax[datax.Length - 1] - datax[0];
            }
        }
        public override string ToString()
        {
            return "Type: V1DataArray; " + base.ToString();
        }
        public override string ToLongString(string format)
        {
            string result = this.ToString() + "\nList:\n";
            for (int i = 0; i < datax.Length; i++)
            {
                result += "x: " + string.Format(format, datax[i])
                    + "; y1: " + string.Format(format, datay[0][i])
                    + "; y2: " + string.Format(format, datay[1][i]) + ";\n";
            }
            return result;
        }
        public override IEnumerator<DataItem> GetEnumerator()
        {
            List<DataItem> res = new List<DataItem>();
            for (int i = 0; i < datax.Length; ++i)
            {
                res.Add(new DataItem(datax[i], datay[0][i], datay[1][i]));
            }
            return res.GetEnumerator();
            //return this.GetV1DataList.GetEnumerator();
        }
        public static bool Save(string filename, V1DataArray arr)
        {
            try
            {

                using (StreamWriter fs = new StreamWriter(filename))
                {
                    fs.WriteLine(JsonSerializer.Serialize(arr.datax));
                    fs.WriteLine(JsonSerializer.Serialize(arr.datay));
                    fs.WriteLine(arr.key);
                    fs.WriteLine(arr.time);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Save error: " + e.Message);
                return false;
            }
            return true;
        }
        public static bool Load(string filename, ref V1DataArray arr)
        {
            try
            {
                string datax, datay, key, time;
                using (StreamReader fs = new StreamReader(filename))
                {
                    datax = fs.ReadLine();
                    datay = fs.ReadLine();
                    key = fs.ReadLine();
                    time = fs.ReadLine();
                }
                double[] dataxarr = JsonSerializer.Deserialize<double[]>(datax);
                double[][] datayarr = JsonSerializer.Deserialize<double[][]>(datay);
                
                arr = new V1DataArray(key, DateTime.Parse(time));
                arr.datax = dataxarr;
                arr.datay = datayarr;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }



    public class V1MainCollection : System.Collections.ObjectModel.ObservableCollection<V1Data>
    {
        public bool Contains(string key)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (this[i].key == key)
                    return true;
            }
            return false;
        }
        public new bool Add(V1Data v1Data)
        {
            if (!this.Contains(v1Data.key))
            {
                base.Add(v1Data);
                return true;
            }
            else
                return false;
        }
        public V1MainCollection(int nV1DataArray, int nV1DataList) //DEBUG CONSTRUCTOR
        {
            double[] xvalues = { 0, 0.2, 0.4, 0.7, 0.8, 1 };
            for (int i = 0; i < nV1DataArray; i++)
            {
                this.Add(new V1DataArray("dataArray" + i.ToString(), DateTime.Now, xvalues,
                    (double x, ref double y1, ref double y2) => { x += i; y1 = x + 1.0; y2 = x + 2.0; }));
            }
            for (int i = 0; i < nV1DataList; i++)
            {
                this.Add(new V1DataList("datalist" + i.ToString(), DateTime.Now, xvalues,
                     (double x) => { return new DataItem(x+=i, x + 1.0, x + 2.0); }));
            }
        }
        public string ToLongString(string format)
        {
            string result = string.Empty;
            foreach (V1Data elem in this)
            {
                result += "\n" + elem.ToLongString(format);
            }
            return result;
        }
        public override string ToString()
        {
            string result = string.Empty;
            foreach (V1Data elem in this)
            {
                result += "\n" + elem.ToString();
            }
            return result;
        }
        public double MeanOfAbs
        {
            get
            {
                if (this.Count == 0) { return double.NaN; };
                IEnumerable<DataItem> items = from coll in this
                                                from item in coll
                                                select item;

                return items.Sum(item => item.Abs()) / items.Count();
            }
        }
        public DataItem? MaxDeviation
        {
            get
            {
                if (this.Count == 0) { return null; };
                double mean = this.MeanOfAbs;
                IEnumerable<V1Data> NotEmpty = from coll in this
                                               where coll.Count() > 0
                                               select coll;

                double MaxDif = NotEmpty.Max(d => d.Max(e => e.Abs() - mean));

                IEnumerable<DataItem> results = from coll in this
                                                from item in coll
                                                where item.Abs() - MaxDif - mean == 0.0
                                                select item;
                DataItem res = results.First();                
                return res;
            }
        }

        public IEnumerable<double> SortedPoints
        {
            get
            {
                if (this.Count == 0) return null;
                IEnumerable<double> X = from coll in this
                                        where coll.Count() > 0
                                        from item in coll
                                        select item.x;
                IEnumerable<double> SortedX = from x in X
                                              orderby x
                                              select x;
                IEnumerable<double> query = SortedX.GroupBy(x => x)
                            .Where(g => g.Count() > 1)
                            .Select(y => y.Key)
                            .ToList();
                return query;
            }
        }

        public IEnumerable<double> OnlyInArrays
        {
            get
            {
                IEnumerable<double> arrX = from coll in this
                                           where coll is V1DataArray
                                           from item in coll
                                           select item.x;

                IEnumerable<double> listX = from coll in this
                                            where coll is V1DataList
                                            from item in coll
                                            select item.x;
                return arrX.Except(listX);
            }
        }
    }   
}