using System;
using System.Globalization;
namespace Mobility
{
    [Serializable]
    public class Polynomial
    {
        // Fixed coefficient convention: a*x^3 + b*x^2 + c*x + d.
        public double A, B, C, D;
        public Polynomial(double a, double b, double c, double d) { A=a; B=b; C=c; D=d; }
        public double Evaluate(double x) { return ((A*x+B)*x+C)*x+D; }
        public void Validate()
        {
            Numeric.RequireFinite(A,"a"); Numeric.RequireFinite(B,"b");
            Numeric.RequireFinite(C,"c"); Numeric.RequireFinite(D,"d");
        }
        public override string ToString()
        { return string.Format(CultureInfo.InvariantCulture,"({0:G4})x³ + ({1:G4})x² + ({2:G4})x + ({3:G4})",A,B,C,D); }
    }
    public static class Numeric
    {
        public static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        public static void RequireFinite(double x,string name)
        { if(!Finite(x)) throw new ArgumentException(name+": требуется конечное число."); }
        public static bool TryParse(string text, out double value)
        { return double.TryParse(text.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out value) && Finite(value); }
    }
}
