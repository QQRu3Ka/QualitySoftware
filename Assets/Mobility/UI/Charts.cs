using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mobility
{
    public class Chart : VisualElement
    {
        public static readonly Color[] Palette = {
            Hex("1D6E72"),Hex("D87A37"),Hex("4B73C4"),Hex("A94C73"),Hex("7D62AF"),Hex("73923D"),
            Hex("D3A12E"),Hex("438EA8"),Hex("B34E3F"),Hex("5D776A"),Hex("B27CA8"),Hex("6F88C9"),
            Hex("96703E"),Hex("918A38"),Hex("428C90"),Hex("65617D") };
        public static Color Hex(string hex) { Color c; ColorUtility.TryParseHtmlString("#"+hex,out c); return c; }
        public double[] Times;
        public double[][] Values;
        public string[] Names;
        public bool[] Visible;
        public bool Radar;
        public double[] Initial, Current;
        public double Minimum, Maximum;
        readonly VisualElement annotations=new VisualElement();
        readonly Label hover=new Label();
        float hoverX=-1;
        public Chart()
        {
            style.minHeight=390; style.flexGrow=1; style.backgroundColor=Color.white;
            style.borderTopLeftRadius=10; style.borderTopRightRadius=10;
            annotations.style.position=Position.Absolute; annotations.style.left=0; annotations.style.right=0;
            annotations.style.top=0; annotations.style.bottom=0; annotations.pickingMode=PickingMode.Ignore;
            Add(annotations); hover.style.position=Position.Absolute; hover.style.top=8; hover.style.left=110;
            hover.style.fontSize=12; hover.style.color=Hex("243940"); hover.pickingMode=PickingMode.Ignore; Add(hover);
            generateVisualContent+=Draw;
            RegisterCallback<GeometryChangedEvent>(_=>RefreshLabels());
            RegisterCallback<PointerMoveEvent>(e=>{
                if(Radar || Times==null || Times.Length<2) return;
                hoverX=e.localPosition.x; var r=PlotRect;
                if(hoverX<r.xMin || hoverX>r.xMax) { hover.text=""; hoverX=-1; MarkDirtyRepaint(); return; }
                double t=Times[0]+(Times[Times.Length-1]-Times[0])*(hoverX-r.xMin)/r.width;
                var sample=new SimulationResult(Times,Values).Sample(t);
                var parts=new List<string> {"t = "+t.ToString("0.000",CultureInfo.InvariantCulture)};
                for(int j=0;j<Names.Length;j++) if(Visible==null || Visible[j]) {
                    parts.Add(Names[j]+": "+sample[j].ToString("0.000",CultureInfo.InvariantCulture));
                    if(parts.Count==5) break;
                }
                hover.text=string.Join("    ",parts); MarkDirtyRepaint();
            });
            RegisterCallback<PointerLeaveEvent>(_=>{hoverX=-1;hover.text="";MarkDirtyRepaint();});
        }
        Rect PlotRect { get { return new Rect(65,42,Math.Max(1,contentRect.width-91),Math.Max(1,contentRect.height-94)); } }
        public void Refresh()
        {
            if(!Radar && Values!=null)
            {
                double min=double.PositiveInfinity,max=double.NegativeInfinity;
                foreach(var row in Values) for(int j=0;j<row.Length;j++) if(Visible==null || Visible[j]) {min=Math.Min(min,row[j]);max=Math.Max(max,row[j]);}
                if(!Numeric.Finite(min)) {min=0;max=1;}
                double pad=Math.Max((max-min)*.08,Math.Max(Math.Abs(max)*.01,.01)); Minimum=min-pad;Maximum=max+pad;
            }
            RefreshLabels(); MarkDirtyRepaint();
        }
        string Format(double x) { return x.ToString(Math.Abs(x)>=10000 || (x!=0 && Math.Abs(x)<.001)?"0.##E+0":"0.###",CultureInfo.InvariantCulture); }
        void Annotation(string text,float x,float y,float width=80)
        {
            var label=new Label(text);label.style.position=Position.Absolute;label.style.left=x;label.style.top=y;
            label.style.width=width;label.style.fontSize=11;label.style.color=Hex("68767C");label.style.unityTextAlign=TextAnchor.MiddleCenter;
            annotations.Add(label);
        }
        void RefreshLabels()
        {
            annotations.Clear(); if(contentRect.width<100) return;
            if(Radar)
            {
                Vector2 center; float radius; RadarGeometry(out center,out radius);
                for(int j=0;j<16;j++) { var point=Point(center,radius+26,j);Annotation("u"+Catalog.Ids[j],point.x-30,point.y-8,60); }
                for(int ring=0;ring<=4;ring++) Annotation(Format(Minimum+(Maximum-Minimum)*ring/4),center.x+5,center.y-radius*ring/4,65);
                return;
            }
            if(Times==null) return;
            var r=PlotRect;
            for(int k=0;k<=5;k++) {
                Annotation(Format(Minimum+(Maximum-Minimum)*k/5),0,r.yMax-r.height*k/5-8,58);
                Annotation(Format(Times[0]+(Times[Times.Length-1]-Times[0])*k/5),r.xMin+r.width*k/5-35,r.yMax+8,70);
            }
            Annotation("Время, t",r.center.x-40,r.yMax+29);
            Annotation("Значение",4,12);
        }
        void Line(Painter2D p,Vector2 a,Vector2 b,Color color,float width=1)
        {p.strokeColor=color;p.lineWidth=width;p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke();}
        void Draw(MeshGenerationContext context)
        {
            var p=context.painter2D;
            if(Radar) {DrawRadar(p);return;} if(Times==null || Values==null) return;
            var r=PlotRect; var grid=Hex("E3E8E8");
            for(int k=0;k<=5;k++) {float x=r.xMin+r.width*k/5,y=r.yMin+r.height*k/5;
                Line(p,new Vector2(x,r.yMin),new Vector2(x,r.yMax),grid);
                Line(p,new Vector2(r.xMin,y),new Vector2(r.xMax,y),grid);}
            Func<double,float> yy=v=>(float)(r.yMax-(v-Minimum)/(Maximum-Minimum)*r.height);
            if(Minimum<0 && Maximum>0) Line(p,new Vector2(r.xMin,yy(0)),new Vector2(r.xMax,yy(0)),Hex("96A5AA"),1.5f);
            for(int j=0;j<Values[0].Length;j++) if(Visible==null || Visible[j])
            {
                p.strokeColor=Palette[j%16];p.lineWidth=j==0?2.7f:1.8f;p.BeginPath();
                // Preserve all samples; bounded by the solver's MaxSteps.
                for(int k=0;k<Times.Length;k++) {var point=new Vector2((float)(r.xMin+(Times[k]-Times[0])/(Times[Times.Length-1]-Times[0])*r.width),yy(Values[k][j]));if(k==0)p.MoveTo(point);else p.LineTo(point);}
                p.Stroke();
            }
            if(hoverX>=r.xMin && hoverX<=r.xMax) Line(p,new Vector2(hoverX,r.yMin),new Vector2(hoverX,r.yMax),Hex("506268"));
        }
        void RadarGeometry(out Vector2 center,out float radius)
        {center=new Vector2(contentRect.width/2,contentRect.height/2);radius=Math.Max(20,Math.Min(contentRect.width,contentRect.height)/2-60);}
        Vector2 Point(Vector2 center,float radius,int index)
        {float angle=-Mathf.PI/2+index*2*Mathf.PI/16;return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;}
        void DrawRadar(Painter2D p)
        {
            if(Initial==null || Current==null) return;
            Vector2 center;float radius;RadarGeometry(out center,out radius);
            for(int j=0;j<16;j++) Line(p,center,Point(center,radius,j),Hex("E1E7E7"));
            for(int ring=1;ring<=4;ring++)
            {
                p.strokeColor=Hex("DCE3E3");p.lineWidth=1;p.BeginPath();
                for(int j=0;j<16;j++){var pt=Point(center,radius*ring/4,j);if(j==0)p.MoveTo(pt);else p.LineTo(pt);}p.ClosePath();p.Stroke();
            }
            var sets=new[]{Initial,Current};
            for(int s=0;s<2;s++) {
                p.strokeColor=s==0?Hex("4B73C4"):Hex("D87A37");p.lineWidth=2.4f;p.BeginPath();
                for(int j=0;j<16;j++){var pt=Point(center,(float)((sets[s][j]-Minimum)/(Maximum-Minimum)*radius),j);if(j==0)p.MoveTo(pt);else p.LineTo(pt);}p.ClosePath();p.Stroke();
            }
        }
    }
}
