using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mobility
{
    public class MobilityApp : MonoBehaviour
    {
        public MobilityModel Model {get;private set;} = new MobilityModel();
        public SimulationResult Result {get;private set;}
        public int ActivePage {get;private set;}
        double start=0,end=1,step=.001;
        VisualElement root,body,nav;
        Label status,summary;
        Button calculate;
        readonly Dictionary<string,string> drafts=new Dictionary<string,string>();
        readonly HashSet<string> invalid=new HashSet<string>();
        readonly string[] pages={"Параметры","Матрица связей","Возмущения","Динамика","Срезы времени","Модель и уравнения"};
        readonly bool[] selected=new bool[16];
        int edgeRow,edgeColumn;
        bool busy;
        public bool HasInvalidInputs { get { return invalid.Count>0; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(FindFirstObjectByType<MobilityApp>()==null &&
                (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SampleScene" || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MobilityCalculator"))
                new GameObject("Mobility Calculator").AddComponent<MobilityApp>();
        }
        void OnEnable() { Initialize(); }
        public void Initialize()
        {
            for(int i=0;i<16;i++)selected[i]=true;
            var document=GetComponent<UIDocument>()??gameObject.AddComponent<UIDocument>();
            var template=Resources.Load<PanelSettings>("MobilityPanel");
            var panel=template!=null?Instantiate(template):ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("MobilityTheme");
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1440,900);panel.match=.5f;
            document.panelSettings=panel;root=document.rootVisualElement;root.Clear();
            var styles=Resources.Load<StyleSheet>("MobilityStyle");if(styles!=null)root.styleSheets.Add(styles);
            root.AddToClassList("app");
            var top=Box(root,"topbar");var brand=Box(top,"brand");Text(brand,"M / 06","eyebrow");Text(brand,"Мобильность ПО","brand-title");
            Text(top,"Лаборатория качества  /  Модель системной динамики","top-note");
            var main=Box(root,"workspace");nav=Box(main,"sidebar");Text(nav,"ИССЛЕДОВАНИЕ","nav-caption");
            for(int i=0;i<pages.Length;i++){int index=i;var button=Btn(nav,(i+1).ToString("00")+"   "+pages[i],()=>ShowPage(index));button.AddToClassList("nav-item");}
            Text(nav,"16 характеристик\n6 внешних факторов\nМетод Рунге–Кутты · RK4","sidebar-note");
            var right=Box(main,"right");var tools=Box(right,"tools");summary=Text(tools,"Начальные параметры готовы","summary");
            Btn(tools,"Сбросить",Reset);calculate=Btn(tools,"Рассчитать →",()=>StartCoroutine(Calculate()));calculate.AddToClassList("primary");
            body=Box(right,"body");status=Text(root,"Готово. Задайте параметры и нажмите «Рассчитать».","status");
            ShowPage(0);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-mobilitySmoke")>=0)StartCoroutine(SmokeCapture());
        }
        public void ShowPage(int index)
        {
            ActivePage=index;body.Clear();int n=0;foreach(var child in nav.Children())if(child is Button){child.EnableInClassList("nav-active",n==index);n++;}
            var head=Box(body,"page-heading");Text(head,pages[index],"page-title");
            string[] descriptions={"Начальные условия, интервал расчёта и функции влияния.","Столбец влияет на строку. Нажатие меняет знак: 0 → +1 → −1.","Шесть функций внешнего воздействия на выбранном интервале.","Изменение характеристик во времени. Выберите линии в легенде.","Сравнение начального состояния с выбранным моментом времени.","Расшифровка переменных, допущения и уравнения текущей матрицы."};
            Text(head,descriptions[index],"muted");
            switch(index){case 0:Parameters();break;case 1:Matrix();break;case 2:Factors();break;case 3:Dynamics();break;case 4:Slices();break;case 5:ModelPage();break;}
        }
        void SetStatus(string message,bool error=false){status.text=message;status.EnableInClassList("error",error);}
        void Changed()
        {
            Result=null;summary.text="Параметры изменены";
            SetStatus(invalid.Count>0?"Исправьте выделенные поля: требуется конечное число.":"Нужен новый расчёт. Результаты предыдущего запуска сброшены.",invalid.Count>0);
            // Charts under a factor editor must not continue showing stale data.
            foreach(var chart in body.Query<Chart>().ToList())chart.style.display=DisplayStyle.None;
        }
        void Reset()
        {if(busy)return;Model=new MobilityModel();start=0;end=1;step=.001;drafts.Clear();invalid.Clear();Result=null;summary.text="Базовый сценарий";SetStatus("Восстановлены исходная матрица и параметры.");ShowPage(ActivePage);}
        IEnumerator Calculate()
        {
            if(busy)yield break;
            if(invalid.Count>0){SetStatus("Исправьте числовые поля. Неверных полей: "+invalid.Count,true);yield break;}
            busy=true;calculate.SetEnabled(false);SetStatus("Вычисление…");yield return null;
            try
            {
                Result=Model.Simulate(start,end,step);
                double last=Result.Values[Result.Values.Length-1][0];summary.text="u6(T) = "+F(last)+"   ·   "+(Result.Times.Length-1)+" шагов";
                bool outside=false,negativeFunction=false;
                foreach(var values in Result.Values) {
                    for(int i=0;i<16;i++){if(values[i]<0 || values[i]>1)outside=true;
                        for(int j=0;j<16;j++)if(Model.A[i,j]!=0 && Model.Links[i,j].Evaluate(values[j])<0)negativeFunction=true;}
                }
                foreach(double t in Result.Times)for(int k=0;k<6;k++){
                    double v=Model.Factors[k].Evaluate(t);if(!Numeric.Finite(v) || Math.Abs(v)>1e12)throw new ArgumentException("F"+(k+1)+": значение вне допустимого численного диапазона.");
                    for(int i=0;i<16;i++)if(Model.B[i,k]!=0 && v<0)negativeFunction=true;
                }
                SetStatus("Расчёт завершён."+(outside?" Есть значения вне [0,1]; они не обрезаны.":"")+(negativeFunction?" Есть отрицательные функции влияния; интерпретация знаков требует проверки.":""));
                ShowPage(ActivePage);
            }
            catch(Exception ex){Result=null;summary.text="Расчёт не выполнен";SetStatus(ex.Message,true);}
            busy=false;calculate.SetEnabled(true);
        }
        ScrollView Scroll(VisualElement parent)
        {var scroll=new ScrollView(ScrollViewMode.Vertical);scroll.AddToClassList("scroll");parent.Add(scroll);return scroll;}
        static VisualElement Box(VisualElement parent,string cls)
        {var v=new VisualElement();v.AddToClassList(cls);parent.Add(v);return v;}
        static Label Text(VisualElement parent,string text,string cls="")
        {var l=new Label(text);if(cls!="")l.AddToClassList(cls);parent.Add(l);return l;}
        static Button Btn(VisualElement parent,string text,Action action)
        {var b=new Button(action){text=text};parent.Add(b);return b;}
        static string F(double x){return x.ToString("0.######",CultureInfo.InvariantCulture);}
        TextField Number(VisualElement parent,string key,double value,Action<double> setter,string label="")
        {
            var field=new TextField(label);field.AddToClassList("number");field.name=key;
            field.SetValueWithoutNotify(drafts.ContainsKey(key)?drafts[key]:F(value));field.EnableInClassList("invalid",invalid.Contains(key));
            field.RegisterValueChangedCallback(e=>{drafts[key]=e.newValue;double parsed;
                if(Numeric.TryParse(e.newValue,out parsed)){invalid.Remove(key);setter(parsed);}else invalid.Add(key);
                field.EnableInClassList("invalid",invalid.Contains(key));Changed();});parent.Add(field);return field;
        }
        void RandomizeInitialValues()
        {
            if(busy)return;
            for(int i=0;i<Model.Initial.Length;i++)
            {
                Model.Initial[i]=UnityEngine.Random.Range(0,1001)/1000.0;
                string key="initial"+i;drafts.Remove(key);invalid.Remove(key);
                var field=body.Q<TextField>(key);
                if(field!=null){field.SetValueWithoutNotify(F(Model.Initial[i]));field.RemoveFromClassList("invalid");}
            }
            Changed();
        }
        void Parameters()
        {
            var scroll=Scroll(body);var range=Box(scroll,"card");Text(range,"Интервал и точность","section-title");
            var row=Box(range,"row");Number(row,"start",start,x=>start=x,"t₀");Number(row,"end",end,x=>end=x,"t₁");Number(row,"step",step,x=>step=x,"h");
            Text(range,"Не более 20 000 шагов. sᵢ > 0 — нормировочный параметр; значения uᵢ не ограничиваются автоматически.","muted");
            var card=Box(scroll,"card");var actions=Box(card,"row");
            var randomize=Btn(actions,"Случайные значения",RandomizeInitialValues);
            randomize.name="randomize-initial";randomize.tooltip="Заполнить все 16 начальных значений uᵢ(t₀) числами от 0 до 1.";
            Text(actions,"uᵢ(t₀): от 0 до 1","muted");
            var header=Box(card,"data-row");Text(header,"Характеристика","variable-name");Text(header,"uᵢ(t₀)","column-title");Text(header,"sᵢ","column-title");
            for(int i=0;i<16;i++) {int index=i;var line=Box(card,"data-row");var label=Text(line,"u"+Catalog.Ids[i]+"  "+Catalog.Names[i],"variable-name");label.tooltip=Catalog.Names[i];
                Number(line,"initial"+i,Model.Initial[i],v=>Model.Initial[index]=v);Number(line,"scale"+i,Model.Scales[i],v=>Model.Scales[index]=v);}
            var influence=Box(scroll,"card");Text(influence,"Функция отдельной связи","section-title");
            Text(influence,"fᵢⱼ(x) = a·x³ + b·x² + c·x + d. Для линейной функции a=b=0; для квадратичной a=0.","muted");
            var editor=Box(influence,"edge-editor");BuildEdgeEditor(editor);
        }
        void BuildEdgeEditor(VisualElement parent)
        {
            parent.Clear();var ids=new List<string>();for(int i=0;i<16;i++)ids.Add("u"+Catalog.Ids[i]+" · "+Catalog.Names[i]);
            var row=new DropdownField("Изменяется",ids,edgeRow);row.AddToClassList("wide-dropdown");parent.Add(row);
            row.RegisterValueChangedCallback(e=>{edgeRow=ids.IndexOf(e.newValue);BuildEdgeEditor(parent);});
            var sources=new List<int>();var labels=new List<string>();for(int j=0;j<16;j++)if(Model.A[edgeRow,j]!=0){sources.Add(j);labels.Add("u"+Catalog.Ids[j]+" → u"+Catalog.Ids[edgeRow]+"  ("+(Model.A[edgeRow,j]>0?"+1":"−1")+")");}
            if(sources.Count==0){Text(parent,"В этой строке нет внутренних связей. Добавьте их в матрице.","muted");return;}
            int selectedIndex=Math.Max(0,sources.IndexOf(edgeColumn));edgeColumn=sources[selectedIndex];
            var source=new DropdownField("Влияет",labels,selectedIndex);parent.Add(source);source.RegisterValueChangedCallback(e=>{edgeColumn=sources[labels.IndexOf(e.newValue)];BuildEdgeEditor(parent);});
            PolyEditor(parent,Model.Links[edgeRow,edgeColumn],"link"+edgeRow+"_"+edgeColumn);
        }
        void PolyEditor(VisualElement parent,Polynomial p,string key)
        {
            var row=Box(parent,"poly-row");Number(row,key+"a",p.A,x=>p.A=x,"a · x³");Number(row,key+"b",p.B,x=>p.B=x,"b · x²");
            Number(row,key+"c",p.C,x=>p.C=x,"c · x");Number(row,key+"d",p.D,x=>p.D=x,"d");
        }
        void Matrix()
        {
            var hint=Box(body,"matrix-hint");Text(hint,"+1  усиливает","positive-text");Text(hint,"−1  ослабляет","negative-text");Text(hint,"0  нет прямой связи","muted");
            var scroll=new ScrollView(ScrollViewMode.VerticalAndHorizontal);scroll.AddToClassList("scroll");body.Add(scroll);
            var table=Box(scroll,"matrix");var top=Box(table,"matrix-row");Text(top,"↓ / →","matrix-label");
            for(int j=0;j<22;j++){var l=Text(top,j<16?"u"+Catalog.Ids[j]:"F"+(j-15),"matrix-label");l.tooltip=j<16?Catalog.Names[j]:Catalog.FactorNames[j-16];}
            for(int i=0;i<16;i++){
                int r=i;var row=Box(table,"matrix-row");var name=Text(row,"u"+Catalog.Ids[i],"matrix-label");name.tooltip=Catalog.Names[i];
                for(int j=0;j<22;j++){
                    int col=j;int v=j<16?Model.A[i,j]:Model.B[i,j-16];var cell=new Button();cell.AddToClassList("matrix-cell");PaintCell(cell,v);
                    cell.tooltip=(j<16?"u"+Catalog.Ids[j]+" · "+Catalog.Names[j]:"F"+(j-15)+" · "+Catalog.FactorNames[j-16])+" → u"+Catalog.Ids[i]+" · "+Catalog.Names[i];
                    if(j==i)cell.SetEnabled(false);
                    cell.clicked+=()=>{int old=col<16?Model.A[r,col]:Model.B[r,col-16];int next=old==0?1:old==1?-1:0;
                        SetMatrixCell(r,col,next);PaintCell(cell,next);};row.Add(cell);
                }
            }
            Text(body,"Матрица составлена для учебной модели. F4–F6 исходно отключены. Наведите курсор на заголовок или ячейку для расшифровки.","footnote");
        }
        static void PaintCell(Button b,int sign){b.text=sign==1?"+1":sign==-1?"−1":"0";b.EnableInClassList("positive",sign==1);b.EnableInClassList("negative",sign==-1);}
        public void SetMatrixCell(int row,int column,int value)
        {
            if(column<16)
            {
                Model.A[row,column]=value;
                if(value==0)foreach(string suffix in new[]{"a","b","c","d"})
                {string key="link"+row+"_"+column+suffix;drafts.Remove(key);invalid.Remove(key);}
            }
            else Model.B[row,column-16]=value;
            Changed();
        }
        bool NeedResult(VisualElement parent)
        {
            if(Result!=null)return false;
            var empty=Box(parent,"empty");Text(empty,"Постройте первый расчёт","section-title");Text(empty,"Нажмите «Рассчитать» вверху экрана. После изменения параметров нужен новый расчёт.","muted");return true;
        }
        void Factors()
        {
            var scroll=Scroll(body);var card=Box(scroll,"card");Text(card,"Fₖ(t) = a·t³ + b·t² + c·t + d","section-title");
            for(int k=0;k<6;k++){var fold=new Foldout{text="F"+(k+1)+"  "+Catalog.FactorNames[k],value=k==0};card.Add(fold);PolyEditor(fold,Model.Factors[k],"factor"+k);}
            if(NeedResult(scroll))return;
            var plot=new Chart();plot.style.height=420;plot.style.flexShrink=0;scroll.Add(plot);
            plot.Times=Result.Times;plot.Values=new double[plot.Times.Length][];plot.Names=new string[6];plot.Visible=new bool[6];
            for(int k=0;k<6;k++){plot.Names[k]="F"+(k+1);plot.Visible[k]=true;}
            for(int i=0;i<plot.Times.Length;i++){plot.Values[i]=new double[6];for(int k=0;k<6;k++)plot.Values[i][k]=Model.Factors[k].Evaluate(plot.Times[i]);}
            Legend(scroll,plot,Catalog.FactorNames);plot.Refresh();
        }
        void Dynamics()
        {
            if(NeedResult(body))return;
            var scroll=Scroll(body);var plot=new Chart{Times=Result.Times,Values=Result.Values,Names=new string[16],Visible=selected};
            for(int i=0;i<16;i++)plot.Names[i]="u"+Catalog.Ids[i];
            plot.style.height=440;plot.style.flexShrink=0;scroll.Add(plot);
            var actions=Box(scroll,"row");Btn(actions,"Все",()=>{for(int i=0;i<16;i++)selected[i]=true;ShowPage(3);});
            Btn(actions,"Только качества",()=>{for(int i=0;i<16;i++)selected[i]=i<6;ShowPage(3);});
            Btn(actions,"Только дефекты",()=>{for(int i=0;i<16;i++)selected[i]=i>=6;ShowPage(3);});
            Legend(scroll,plot,Catalog.Names);plot.Refresh();
        }
        void Legend(VisualElement parent,Chart chart,string[] fullNames)
        {
            var legend=Box(parent,"legend");for(int j=0;j<chart.Names.Length;j++){
                int index=j;var toggle=new Toggle(chart.Names[j]+"  "+fullNames[j]);toggle.value=chart.Visible[j];toggle.AddToClassList("legend-item");
                toggle.style.color=Chart.Palette[j];toggle.tooltip=fullNames[j];toggle.RegisterValueChangedCallback(e=>{chart.Visible[index]=e.newValue;chart.Refresh();});legend.Add(toggle);
            }
        }
        void Slices()
        {
            if(NeedResult(body))return;
            var scroll=Scroll(body);var controls=Box(scroll,"row");var plot=new Chart{Radar=true,Initial=Result.Values[0],Current=Result.Sample(start)};
            plot.style.height=420;plot.style.flexShrink=0;plot.Minimum=0;plot.Maximum=1;
            foreach(var row in Result.Values)foreach(var v in row){plot.Minimum=Math.Min(plot.Minimum,v);plot.Maximum=Math.Max(plot.Maximum,v);}
            var title=Text(scroll,"","section-title");var swatches=Box(scroll,"row");Text(swatches,"● Начальное состояние","initial-key");Text(swatches,"● Выбранный момент","current-key");
            scroll.Add(plot);var table=Box(scroll,"card");
            Action<double> update=t=>{plot.Current=Result.Sample(t);title.text="Состояние при t = "+F(t);plot.Refresh();table.Clear();
                var head=Box(table,"data-row");Text(head,"Характеристика","variable-name");Text(head,"Начало","column-title");Text(head,"Выбрано","column-title");
                for(int j=0;j<16;j++){var r=Box(table,"data-row");Text(r,"u"+Catalog.Ids[j]+"  "+Catalog.Names[j],"variable-name");Text(r,F(plot.Initial[j]),"column-title");Text(r,F(plot.Current[j]),"column-title");}};
            for(int k=0;k<=4;k++){double t=start+(end-start)*k/4;Btn(controls,new[]{"Начало","¼ интервала","½ интервала","¾ интервала","Конец"}[k],()=>update(t));}
            Text(scroll,"Шкала общая для всех срезов и всей траектории. При отрицательных значениях центр соответствует минимуму шкалы, а не нулю.","footnote");update(start);
        }
        void ModelPage()
        {
            var scroll=Scroll(body);var card=Box(scroll,"card");Text(card,"Модель мобильности · u6","section-title");
            Text(card,"Источник названий: таблица 2.1 диссертации, с. 99, 100 и 106. Матрица связей — согласованная учебная модель; коэффициенты требуют экспертной настройки.","paragraph");
            Text(card,"Качества u6, u29–u33: больше — лучше. Дефекты u126–u135: больше — хуже. Произведения описывают совместное влияние; нулевой множитель обнуляет весь член. sᵢ — положительная постоянная нормировки, не текущее uᵢ.","paragraph");
            Text(card,"f[i,j](x) = a·x³ + b·x² + c·x + d; Fₖ — внешние факторы. Пустой внутренний продукт равен 1. Если у члена нет ни связей, ни факторов, он равен 0. Отсутствующий внешний множитель равен 1.","paragraph");
            Text(card,"Решение — безразмерные показатели, не вероятности. Значения вне [0,1] не обрезаются. При отрицательных функциях знак связи не гарантирует монотонность влияния.","paragraph");
            for(int i=0;i<16;i++){var item=Box(scroll,"card");Text(item,"u"+Catalog.Ids[i]+" · "+Catalog.Names[i],"section-title");Text(item,Model.Equation(i),"equation");}
        }
        IEnumerator SmokeCapture()
        {
            yield return new WaitForSeconds(2);yield return Calculate();
            string folder=System.IO.Path.Combine(Application.dataPath,"..","SmokeCaptures");System.IO.Directory.CreateDirectory(folder);
            for(int i=0;i<6;i++){ShowPage(i);yield return new WaitForSeconds(.7f);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"screen-"+i+".png"));yield return new WaitForSeconds(.5f);}
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"result.txt"),Result==null?"FAILED":"PASS u6(T)="+F(Result.Sample(end)[0]));
            Application.Quit();
        }
    }
}
