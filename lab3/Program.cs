namespace lab3
{
    using Doronin_LAB_2;
    using System.Runtime.InteropServices;
    internal class Program
    {
        static void Main(string[] args)
        {
            int N = 10;
            int M = 5;
            int MaxIterations = 10000;

            V1DataArray a1 = new V1DataArray("v1dataarray", DateTime.Now, N, 0.0, 1.0,
                (double x, ref double y1, ref double y2) => { y1 = x*x*x; y2 = 0.0; });

            SplineData s1 = new SplineData(a1, M, MaxIterations);
            s1.Interpolate();
            Console.WriteLine(s1.ToLongString("{0:f5}"));

        }
           
    }
}