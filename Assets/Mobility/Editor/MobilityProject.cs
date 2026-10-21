using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Mobility.Editor
{
    [InitializeOnLoad]
    public static class MobilityProject
    {
        public const string ScenePath="Assets/Scenes/MobilityCalculator.unity";
        static readonly string Project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        static MobilityProject(){EditorApplication.update+=ProcessRequest;}
        static void ProcessRequest()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
            string path=Path.Combine(Project,"Temp","mobility-command.txt");
            if(!File.Exists(path))return;
            string command=File.ReadAllText(path).Trim();File.Delete(path);
            try
            {
                if(command=="verify")Verify();
                else if(command=="windows")BuildWindows();
                else if(command=="webgl")BuildWebGL();
                else if(command=="setup")Setup();
            }
            catch(Exception ex){Directory.CreateDirectory(Path.Combine(Project,"TestResults"));File.WriteAllText(Path.Combine(Project,"TestResults","unity-error.txt"),ex.ToString());Debug.LogException(ex);}
        }
        [MenuItem("Mobility/1. Prepare calculator scene")]
        public static void Setup()
        {
            const string panelPath="Assets/Mobility/Resources/MobilityPanel.asset";
            if(AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath)==null)
            {
                var panel=ScriptableObject.CreateInstance<PanelSettings>();
                panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("MobilityTheme");
                panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution=new Vector2Int(1440,900);panel.match=.5f;
                AssetDatabase.CreateAsset(panel,panelPath);
            }
            if(!File.Exists(ScenePath))
            {
                Scene previous=SceneManager.GetActiveScene();
                Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                new GameObject("Mobility Calculator").AddComponent<MobilityApp>();
                var camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Chart.Hex("F1F3F1");camera.orthographic=true;
                EditorSceneManager.SaveScene(scene,ScenePath);
                SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.productName="Мобильность ПО";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Mobility/2. Verify model and interface")]
        public static void Verify()
        {
            Setup();var model=new MobilityModel();var result=model.Simulate(0,1,.001);
            if(Math.Abs(result.Sample(1)[0]-.508274)>1e-6)throw new Exception("Unexpected model result");
            if(Resources.Load<UnityEngine.UIElements.StyleSheet>("MobilityStyle")==null)throw new Exception("Missing stylesheet");
            // Construct every screen against the real Unity UI Toolkit implementation.
            var obj=new GameObject("Temporary interface verification");
            try {
                var app=obj.AddComponent<MobilityApp>();app.Initialize();
                for(int i=0;i<6;i++)app.ShowPage(i);
                app.ShowPage(0);
                var field=obj.GetComponent<UIDocument>().rootVisualElement.Q<TextField>("link0_1d");
                if(field==null)throw new Exception("Missing edge coefficient editor");
                field.value="invalid";
                if(!app.HasInvalidInputs)throw new Exception("Invalid input not detected");
                app.SetMatrixCell(0,1,0);
                if(app.HasInvalidInputs)throw new Exception("Disabled invalid link blocks calculation");
                app.ShowPage(0);
            }
            finally {UnityEngine.Object.DestroyImmediate(obj);}
            Directory.CreateDirectory(Path.Combine(Project,"TestResults"));
            File.WriteAllText(Path.Combine(Project,"TestResults","unity-verification.txt"),"PASS\nUnity "+Application.unityVersion+"\nCatalog: 16 variables\nDefault u6(T): "+result.Sample(1)[0]+"\nStylesheet: loaded\nAll six screens: constructed\nInvalid input and disabled-link regression: passed\n");
            Debug.Log("Mobility verification passed");
        }
        [MenuItem("Mobility/3. Build Windows")]
        public static void BuildWindows(){Verify();Build(BuildTarget.StandaloneWindows64,Path.Combine(Project,"Builds","Windows","MobilityCalculator.exe"));}
        [MenuItem("Mobility/4. Build WebGL")]
        public static void BuildWebGL()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("Install WebGL Build Support for Unity 6000.2.14f1 through Unity Hub.");
            Setup();PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            Build(BuildTarget.WebGL,Path.Combine(Project,"Builds","WebGL"));
        }
        static void Build(BuildTarget target,string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=destination,target=target,options=BuildOptions.None});
            string log="Result: "+report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nDuration: "+report.summary.totalTime+"\nOutput: "+destination;
            Directory.CreateDirectory(Path.Combine(Project,"TestResults"));File.WriteAllText(Path.Combine(Project,"TestResults","build-"+target+".txt"),log);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception(log);
        }
    }
}
