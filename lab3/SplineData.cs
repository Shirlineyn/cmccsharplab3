using Doronin_LAB_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace lab3
{
    public struct SplineDataItem
    {
        public double x { get; set; }
        public double y1 { get; set; }
        public double y2 { get; set; }
        public SplineDataItem(double x, double y1, double y2)
        {
            this.x = x;
            this.y1 = y1;
            this.y2 = y2;
        }
        public string ToString(string format)
        {
            return string.Format(format, x, y1, y2);
        }
        public override string ToString()
        {
            return "X is " + x.ToString() + "; Y1 is " + y1.ToString() + "; Y2 is " + y2.ToString() + ".";
        }
    }

    public class SplineData
    {
        public V1DataArray BaseArray {  get; set; }
        public int SplinePointsN { get; set; }
        public double[] SplineValuesArr { get; set; }
        public int MaxIterations {  get; set; }
        public int StopCondition { get; set; }
        public double ResidualError { get; set; }
        public List<SplineDataItem> SplineValues {  get; set; }
        public SplineData(V1DataArray BaseArray, int SplinePointsN, int MaxIterations)
        {
            this.BaseArray = BaseArray;
            this.SplinePointsN = SplinePointsN;
            this.MaxIterations = MaxIterations;
            this.SplineValues = new List<SplineDataItem>();
            this.SplineValuesArr = new double[BaseArray.datax.Length];
        }
        public int Interpolate()
        {
            int baseN = this.BaseArray.datax.Length;

            double[] SplineMValues = new double[this.SplinePointsN];
            double[] SplineError = new double[1];
            int error = 666;
            int lStopCondition = 666;
            
            double[] SplineValues = new double[3 * this.SplinePointsN];
            int result = Lab3OptimizeSpline(baseN, this.BaseArray.datax, this.BaseArray.datay[0], 
                this.SplinePointsN , ref lStopCondition,
                this.SplineValuesArr, SplineMValues, SplineError, ref error, this.MaxIterations);

            // равномерная сетка
            double[] uniformX = new double[this.SplinePointsN];
            double step = this.BaseArray.datax[this.BaseArray.datax.Length - 1] - this.BaseArray.datax[0];
            for(int i = 0; i < this.SplinePointsN; i++)
            {
                uniformX[i] = this.BaseArray.datax[0] + step * i;
            }

            for (int i = 0; i < this.BaseArray.datax.Length; i++)
            {
                this.SplineValues.Add(new SplineDataItem(this.BaseArray.datax[i], this.BaseArray.datay[0][i], this.SplineValuesArr[i]));
            }
            this.ResidualError = SplineError[0];
            this.StopCondition = lStopCondition;
            return 0;
        }
        private string getStopCondition(int cond)
        {
            switch(cond)
            {
                case 1: return "Превышено заданное число итераций";
                case 2: return "Размер доверительной области < E";
                case 3: return "Норма невязки < E";
                case 4: return "Норма строк матрицы Якоби < E";
                case 5: return "Пробный шаг < E";
                case 6: return "разность нормы функции и погрешности < E";
                default: return "N/A";
            }       
        }
        public string ToLongString(string format)
        {
            const string tabsize = "{0,12:f5}";
            string result = "";
            result += "V1DataArray info:\n" + this.BaseArray.ToLongString(format);
            result += "Spline info:\n";
            result += string.Format(tabsize, "X") + string.Format(tabsize, "Ytrue")
                    + string.Format(tabsize, "Yestimated") + "\n";
            foreach (SplineDataItem item in this.SplineValues)
            {
                result += string.Format(tabsize, item.x);
                result += string.Format(tabsize, item.y1);
                result += string.Format(tabsize, item.y2) + "\n";
            }
            result += "MaxIterations: " + this.MaxIterations + "\n";
            result += "StopCondition: " + getStopCondition(this.StopCondition) + "\n";
            result += "ResidualError: " + this.ResidualError + "\n";
            return result;
        }
        public bool Save(string filename, string format)
        {
            try 
            { 
                using (StreamWriter fs = new StreamWriter(filename))
                {
                    fs.Write(this.ToLongString(format));
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
        public static void Debug_Test()
        {
            // Узлы сплайна
            double xL = 0; // левый конец отрезка
            double xR = 1.0; // правый конец отрезка
            const int nX = 5; // число узлов сплайна
            double[] X = new double[nX]; // массив узлов сплайна
                                         // Равномерная сетка на отрезке [xL, xR]
            double hX = (xR - xL) / (nX - 1); // шаг сетки
            X[0] = xL;
            for (int j = 1; j < nX; j++) X[j] = X[0] + hX * j;

            int nY = 1; // размерность векторной функции
            double[] Y = new double[nX]; // массив заданных значений векторной функции
            for (int j = 0; j < nX; j++) Y[j] = X[j] * X[j] * X[j];
            double d1L = 0; // значение первой производной сплайна на левом конце
            double d1R = 3; // значение первой производной сплайна на правом конце
                            // Равномерная сетка, на которой вычисляются значения сплайна и производных
            int nS = 9; // число узлов равномерной сетки
            double sL = xL; // левый конец отрезка
            double sR = xR; // правый конец отрезка

            // Массив узлов на отрезке [sL, sR]
            double[] sites = new double[nS];
            double hS = (sR - sL) / (nS - 1); // шаг сетки
            sites[0] = sL;
            for (int j = 0; j < nS; j++) sites[j] = sites[0] + hS * j;

            double[] SplineValues = new double[3 * nS]; // массив вычисленных значений
                                                        // сплайна и его производных
            double limitL = xL; // левый конец отрезка интегрирования
            double limitR = xR / 2; // правый конец отрезка интегрирования
            double[] integrals = new double[1]; // значение интеграла
            try
            {
                int ret = CubicSplineTest(
            nX, // число узлов сплайна
            X, // массив узлов сплайна
            nY, // размерность векторной функции
            Y, // массив заданных значений векторной функции
            d1L, // производная сплайна на левом конце
            d1R, // производная сплайна на правом конце
            nS, // число узлов равномерной сетки,на которой
                // вычисляются значения сплайна и его производных
            sL, // левый конец равномерной сетки
            sR, // правый конец равномерной сетки
            SplineValues, // массив вычисленных значений сплайна и производных
            limitL, // левый конец отрезка интегрирования
            limitR, // правый конец отрезка интегрирования
            integrals); // значение интеграла
                Console.WriteLine($"ret = {ret}\n");
                Console.WriteLine($"Заданные значения в узлах сплайна");
                for (int j = 0; j < nX; j++)
                    Console.WriteLine($"X[{j}] = {X[j].ToString("F3")}" +
                    $" Y[{j}] = {Y[j].ToString("F6")}");
                Console.WriteLine($"\nВычисленные значения сплайна и производных");
                Console.WriteLine($"\nУзел Значение Первая Вторая");
                Console.WriteLine($"сетки сплайна производная производная");
                for (int j = 0; j < nS; j++)
                    Console.WriteLine($"{sites[j].ToString("F3")}" +
                    $" {SplineValues[3 * j].ToString("F6")}" +
                    $" {SplineValues[3 * j + 1].ToString("F6")} " +
                    $" {SplineValues[3 * j + 2].ToString("F2")}");
                Console.WriteLine($"\nВычисленное значение интеграла {integrals[0]}");
                Console.WriteLine($"Точное значение интеграла {1.0 / 64}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сплайн-интерполяции\n{ex}");
            }
        }

        [DllImport("lab3_dll.dll",
        CallingConvention = CallingConvention.Cdecl)]
        public static extern
        int CubicSplineTest(int nX, double[] X, int nY, double[] Y, double d1L, double d1R,
                        int nS, double sL, double sR, double[] splineValues,
                        double limitL, double limitR, double[] integrals);

        
        [DllImport("lab3_dll.dll",
        CallingConvention = CallingConvention.Cdecl)]
        public static extern
        int Lab3OptimizeSpline(int nX, double[] X, double[] Y, int M, ref int StopCondition, 
            double[] splineBaseXValues, double[] splineValues, double[] splineError, ref int error,
            int MaxIterations);
    }
   

}
