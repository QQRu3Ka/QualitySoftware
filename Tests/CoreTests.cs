using System;
using Mobility;

class CoreTests
{
    static int count;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
    static void Near(double actual, double expected, double tolerance, string name) { Check(Math.Abs(actual - expected) < tolerance, name); }
    static void Reject(Action action, string name) { try { action(); } catch (ArgumentException) { Check(true, name); return; } throw new Exception(name); }
    static void Main()
    {
        var constant = Rk4.Solve((t,y) => new [] { 2.0 }, new [] { 3.0 }, 0, 1, .3);
        Near(constant.Times[constant.Times.Length-1], 1, 1e-12, "Exact final time for partial last step");
        Near(constant.Sample(.25)[0], 3.5, 1e-10, "Quarter sample interpolation");
        Near(constant.Sample(1)[0], 5, 1e-10, "Constant derivative analytic solution");
        var exp = Rk4.Solve((t,y) => new [] { y[0] }, new [] { 1.0 }, 0, 1, .1);
        var fine = Rk4.Solve((t,y) => new [] { y[0] }, new [] { 1.0 }, 0, 1, .05);
        double err = Math.Abs(exp.Sample(1)[0]-Math.E), fineErr = Math.Abs(fine.Sample(1)[0]-Math.E);
        Check(err/fineErr > 14, "Fourth-order convergence");
        Reject(() => Rk4.Solve((t,y)=>y,new[]{1.0},0,1,0), "Reject zero step");
        Reject(() => Rk4.Solve((t,y)=>y,new[]{1.0},0,1,1e-9), "Bound computation size");
        Reject(() => Rk4.Solve((t,y)=>new[]{double.NaN},new[]{1.0},0,1,.1), "Reject divergent derivative");
        var model = new MobilityModel();
        Check(Catalog.Ids.Length==16 && Catalog.Ids[15]==135, "Exact variable catalog");
        var y0 = new double[16]; for(int i=0;i<16;i++) y0[i]=.5;
        var baseRhs = model.Derivative(0,y0);
        Near(baseRhs[0],(.03125*1.5-.0009765625*.2)/10,1e-12,"Approved u6 equation");
        model.Factors[3].D=999; model.Factors[4].D=-999; model.Factors[5].D=999;
        var rhs=model.Derivative(0,y0);
        for(int i=0;i<16;i++) Near(rhs[i],baseRhs[i],1e-12,"Disconnected factor "+i);
        for(int i=0;i<16;i++) { model.Scales[i]=1; for(int j=0;j<16;j++) model.A[i,j]=0; for(int k=0;k<6;k++) model.B[i,k]=0; }
        model.A[0,1]=1; y0[1]=.7;
        rhs=model.Derivative(0,y0);
        Near(rhs[0],.7,1e-12,"Column influences row with neutral empty factor");
        Near(rhs[1],0,1e-12,"Empty positive and negative terms equal zero");
        model.A[0,1]=-1; Near(model.Derivative(0,y0)[0],-.7,1e-12,"Negative edge sign");
        model.Scales[0]=0; Reject(()=>model.Validate(),"Reject zero normalization scale");
        var p = new Polynomial(1,2,3,4); Near(p.Evaluate(2),26,1e-12,"Polynomial Horner evaluation");
        var demo = new MobilityModel(); var run=demo.Simulate(0,1,.001);
        Near(run.Sample(1)[0],.508274,1e-6,"Approved demonstration reference");
        Console.WriteLine("ALL PASSED: "+count);
    }
}
