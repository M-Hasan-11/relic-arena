using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RelicArena;

public static class RelicBuilder
{
    static readonly Dictionary<string,Color> colors=new Dictionary<string,Color>
    {
        {"Basalt",new Color(.065f,.10f,.14f)}, {"Slate",new Color(.16f,.23f,.28f)},
        {"Stone",new Color(.30f,.38f,.40f)}, {"Sandstone",new Color(.57f,.52f,.40f)},
        {"Gold",new Color(.75f,.46f,.16f)}, {"DarkGold",new Color(.31f,.21f,.10f)},
        {"Teal",new Color(.025f,.40f,.43f)}, {"Cloth",new Color(.035f,.16f,.20f)},
        {"Glow",new Color(.15f,.95f,.83f)}, {"Red",new Color(.70f,.12f,.16f)},
        {"RedGlow",new Color(1,.16f,.09f)}, {"Obsidian",new Color(.10f,.045f,.075f)},
        {"Steel",new Color(.53f,.70f,.74f)}
    };
    static Dictionary<string,Material> materials;
    public static void BuildAll(){Setup();BuildWindows();}
    [MenuItem("Relic Arena/Rebuild Scene")]
    public static void Setup()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before rebuilding.");
        Directory.CreateDirectory("Assets/Materials");Directory.CreateDirectory("Assets/Prefabs");Directory.CreateDirectory("Assets/Scenes");
        materials=new Dictionary<string,Material>();
        foreach(var pair in colors)
        {
            string path="Assets/Materials/"+pair.Key+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=pair.Value;m.SetFloat("_Metallic",pair.Key=="Gold"||pair.Key=="Steel"?.65f:.12f);
            m.SetFloat("_Smoothness",.36f);
            if(pair.Key.Contains("Glow")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",pair.Value*3);}
            materials[pair.Key]=m;
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var environment=Model("TempleArena");environment.name="Sky Temple / Blender environment";
        // FBX transforms are baked into Unity's metres during import.
        var floor=new GameObject("Walkable arena / radius 16.5m");
        var collider=floor.AddComponent<BoxCollider>();collider.size=new Vector3(36,.2f,36);collider.center=new Vector3(0,-.11f,0);
        var hero=Model("Guardian");var heroPrefab=PrefabUtility.SaveAsPrefabAsset(hero,"Assets/Prefabs/Guardian.prefab");UnityEngine.Object.DestroyImmediate(hero);
        var enemy=Model("Sentinel");var enemyPrefab=PrefabUtility.SaveAsPrefabAsset(enemy,"Assets/Prefabs/Sentinel.prefab");UnityEngine.Object.DestroyImmediate(enemy);
        var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";
        var camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();
        camera.fieldOfView=57;camera.nearClipPlane=.15f;camera.farClipPlane=180;
        camera.transform.position=new Vector3(20,14,-23);camera.transform.LookAt(new Vector3(0,1,0));
        camera.clearFlags=CameraClearFlags.Skybox;camera.backgroundColor=new Color(.12f,.20f,.28f);
        camera.allowHDR=true;
        var cameraData=camera.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;
        cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(pipeline){pipeline.supportsHDR=true;pipeline.msaaSampleCount=4;EditorUtility.SetDirty(pipeline);}
        string skyPath="Assets/Materials/TempleSky.mat";
        var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if(!sky){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,skyPath);}
        sky.SetColor("_SkyTint",new Color(.5f,.5f,.5f));sky.SetColor("_GroundColor",new Color(.12f,.18f,.26f));
        sky.SetFloat("_Exposure",.8f);sky.SetFloat("_AtmosphereThickness",1.2f);RenderSettings.skybox=sky;
        Light("Sun / late amber",new Vector3(48,-32,0),new Color(1,.80f,.58f),2.2f);
        Light("Moon / cool fill",new Vector3(35,150,0),new Color(.32f,.67f,1),1.1f,false);
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.46f,.58f,.70f);RenderSettings.ambientEquatorColor=new Color(.32f,.40f,.46f);RenderSettings.ambientGroundColor=new Color(.16f,.20f,.23f);
        DynamicGI.UpdateEnvironment();
        RenderSettings.fog=true;RenderSettings.fogColor=camera.backgroundColor;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.014f;
        var volumeObject=new GameObject("Atmosphere");var volume=volumeObject.AddComponent<Volume>();volume.isGlobal=true;
        string profilePath="Assets/Materials/Atmosphere.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
        if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
        bloom.intensity.Override(.65f);bloom.threshold.Override(1.05f);bloom.scatter.Override(.7f);
        if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}
        vignette.intensity.Override(.22f);vignette.smoothness.Override(.5f);
        if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}
        tone.mode.Override(TonemappingMode.ACES);volume.sharedProfile=profile;
        var game=new GameObject("Relic Arena / Game Director").AddComponent<RelicGame>();
        game.guardianPrefab=heroPrefab;game.sentinelPrefab=enemyPrefab;game.view=camera;
        game.tealFx=materials["Glow"];game.redFx=materials["RedGlow"];game.goldFx=materials["Gold"];
        StoreAssetsBuilder.UpgradeScene(game,environment);
        RealmsBuilder.Build(game,environment);
        PlayerSettings.companyName="Relic Studio";PlayerSettings.productName="Relic Arena";
        PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        QualitySettings.shadowDistance=70;QualitySettings.shadows=UnityEngine.ShadowQuality.All;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/RelicArena.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/RelicArena.unity",true)};
        AssetDatabase.SaveAssets();
        Debug.Log("RELIC_SETUP_COMPLETE");
    }
    static GameObject Model(string name)
    {
        string path="Assets/Art/"+name+".fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new Exception("Missing Blender model: "+path);
        importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        var o=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));o.name=name;
        foreach(var r in o.GetComponentsInChildren<Renderer>())
        {
            var list=r.sharedMaterials;
            for(int i=0;i<list.Length;i++)
            {
                if(!list[i])continue;string key=list[i].name.Split('.')[0];
                if(materials.TryGetValue(key,out var material))list[i]=material;
            }
            r.sharedMaterials=list;
        }
        return o;
    }
    static void Light(string name,Vector3 angle,Color color,float intensity,bool shadows=true)
    {
        var o=new GameObject(name);o.transform.rotation=Quaternion.Euler(angle);
        var l=o.AddComponent<Light>();l.type=LightType.Directional;l.color=color;l.intensity=intensity;
        l.shadows=shadows?LightShadows.Soft:LightShadows.None;
    }
    [MenuItem("Relic Arena/Build Windows")]
    public static void BuildWindows()
    {
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/Windows/RelicArena.exe"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{"Assets/Scenes/RelicArena.unity"},locationPathName=output,
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
        });
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/build-result.txt")),report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nBytes: "+report.summary.totalSize);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("RELIC_BUILD_COMPLETE "+output);
    }
}
