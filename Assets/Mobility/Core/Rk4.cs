using System;
namespace Mobility
{
    public class SimulationResult
    {
        public readonly double[] Times;
        public readonly double[][] Values;
        public SimulationResult(double[] times,double[][] values) { Times=times; Values=values; }
        public double[] Sample(double time)
        {
            Numeric.RequireFinite(time,"Время среза");
            if(time<=Times[0]) return (double[])Values[0].Clone();
            if(time>=Times[Times.Length-1]) return (double[])Values[Values.Length-1].Clone();
            int index=Array.BinarySearch(Times,time);
            if(index>=0) return (double[])Values[index].Clone();
            int hi=~index,lo=hi-1; double weight=(time-Times[lo])/(Times[hi]-Times[lo]);
            var result=new double[Values[lo].Length];
            for(int i=0;i<result.Length;i++) result[i]=Values[lo][i]*(1-weight)+Values[hi][i]*weight;
            return result;
        }
    }
    public static class Rk4
    {
        public const int MaxSteps=20000;
        const double Limit=1e12;
        public static SimulationResult Solve(Func<double,double[],double[]> rhs,double[] initial,double start,double end,double step)
        {
            Numeric.RequireFinite(start,"Начало"); Numeric.RequireFinite(end,"Конец"); Numeric.RequireFinite(step,"Шаг");
            if(end<=start || step<=0) throw new ArgumentException("Нужно t₁ > t₀ и h > 0.");
            double raw=Math.Ceiling((end-start)/step);
            if(!Numeric.Finite(raw) || raw>MaxSteps) throw new ArgumentException("Слишком много шагов. Допустимо не более 20 000; увеличьте h.");
            if(start+Math.Min(step,end-start)==start) throw new ArgumentException("Шаг слишком мал для выбранного времени.");
            int count=(int)raw;
            var times=new double[count+1]; var values=new double[count+1][];
            times[0]=start; values[0]=(double[])initial.Clone(); Check(values[0]);
            for(int n=0;n<count;n++)
            {
                double t=times[n],next=n==count-1?end:Math.Min(end,start+(n+1)*step),h=next-t;
                var y=values[n]; var a=rhs(t,y); Check(a);
                var b=rhs(t+h/2,Add(y,a,h/2)); Check(b);
                var c=rhs(t+h/2,Add(y,b,h/2)); Check(c);
                var d=rhs(next,Add(y,c,h)); Check(d);
                var z=new double[y.Length];
                for(int j=0;j<z.Length;j++) z[j]=y[j]+h*(a[j]+2*b[j]+2*c[j]+d[j])/6;
                Check(z); times[n+1]=next; values[n+1]=z;
            }
            return new SimulationResult(times,values);
        }
        static double[] Add(double[] y,double[] k,double h)
        { var z=new double[y.Length]; for(int i=0;i<z.Length;i++) z[i]=y[i]+h*k[i]; Check(z); return z; }
        static void Check(double[] values)
        {
            foreach(double x in values) if(!Numeric.Finite(x) || Math.Abs(x)>Limit)
                throw new ArgumentException("Расчёт остановлен: нечисловое значение или модуль > 10¹². Уменьшите шаг и проверьте коэффициенты.");
        }
    }
}
