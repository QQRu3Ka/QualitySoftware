using System;
using System.Collections.Generic;
namespace Mobility
{
    public class MobilityModel
    {
        public int[,] A=Catalog.CreateA(), B=Catalog.CreateB();
        public readonly double[] Initial=new double[16], Scales=new double[16];
        public readonly Polynomial[,] Links=new Polynomial[16,16];
        public readonly Polynomial[] Factors={ new Polynomial(0,1,0,1),new Polynomial(0,0,0,.5),
            new Polynomial(0,0,0,.2),new Polynomial(0,0,2,-1),new Polynomial(0,0,0,.3),new Polynomial(0,0,0,.4) };
        public MobilityModel()
        {
            for(int i=0;i<16;i++) { Initial[i]=.5; Scales[i]=10;
                for(int j=0;j<16;j++) Links[i,j]=new Polynomial(0,0,1,0); }
        }
        public void Validate()
        {
            for(int i=0;i<16;i++)
            {
                Numeric.RequireFinite(Initial[i],"u"+Catalog.Ids[i]);
                Numeric.RequireFinite(Scales[i],"s"+Catalog.Ids[i]);
                if(Scales[i]<=0) throw new ArgumentException("Все нормировочные параметры s должны быть больше нуля.");
                for(int j=0;j<16;j++) { if(Math.Abs(A[i,j])>1) throw new ArgumentException("Матрица A: только −1, 0, +1."); Links[i,j].Validate(); }
                for(int j=0;j<6;j++) if(Math.Abs(B[i,j])>1) throw new ArgumentException("Матрица B: только −1, 0, +1.");
            }
            foreach(var factor in Factors) factor.Validate();
        }
        public double[] Derivative(double t,double[] y)
        {
            var f=new double[6]; for(int k=0;k<6;k++) f[k]=Factors[k].Evaluate(t);
            var result=new double[16];
            for(int i=0;i<16;i++) result[i]=(Term(i,1,y,f)-Term(i,-1,y,f))/Scales[i];
            return result;
        }
        double Term(int i,int sign,double[] y,double[] f)
        {
            double product=1,sum=0; bool hasLink=false,hasFactor=false;
            for(int j=0;j<16;j++) if(A[i,j]==sign) { hasLink=true; product*=Links[i,j].Evaluate(y[j]); }
            for(int k=0;k<6;k++) if(B[i,k]==sign) { hasFactor=true; sum+=f[k]; }
            if(!hasLink && !hasFactor) return 0;
            return product*(hasFactor?sum:1);
        }
        public SimulationResult Simulate(double start,double end,double step)
        { Validate(); return Rk4.Solve(Derivative,Initial,start,end,step); }
        public string Equation(int row)
        { return "du"+Catalog.Ids[row]+"/dt = ["+TermText(row,1)+" − "+TermText(row,-1)+"] / s"+Catalog.Ids[row]; }
        string TermText(int row,int sign)
        {
            var links=new List<string>(); var factors=new List<string>();
            for(int j=0;j<16;j++) if(A[row,j]==sign) links.Add("f["+Catalog.Ids[row]+","+Catalog.Ids[j]+"](u"+Catalog.Ids[j]+")");
            for(int k=0;k<6;k++) if(B[row,k]==sign) factors.Add("F"+(k+1));
            if(links.Count==0 && factors.Count==0) return "0";
            if(factors.Count>0) links.Add("("+string.Join(" + ",factors.ToArray())+")");
            return string.Join(" · ",links.ToArray());
        }
    }
}
