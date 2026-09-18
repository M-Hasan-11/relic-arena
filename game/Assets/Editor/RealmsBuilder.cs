using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using RelicArena;
public static class RealmsBuilder
{
    const string Blink="Assets/Blink/Art/Animations/Animations_Starter_Pack/";
    static AnimationClip Clip(string path,bool loop)
    {
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType=ModelImporterAnimationType.Human;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;importer.SaveAndReimport();
        var clips=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
        foreach(var c in clips){c.loopTime=loop;c.loopPose=loop;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionXZ=true;c.heightFromFeet=true;}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
        if(!clip.humanMotion)throw new Exception("Not a humanoid clip: "+path);
        return clip;
    }
    static void State(AnimatorStateMachine machine,string name,AnimationClip clip,float duration=0)
    {var state=machine.AddState(name);state.motion=clip;state.speed=duration>0?clip.length/duration:1;}
    public static void Build(RelicGame game,GameObject environment)
    {
        var idle=Clip("Assets/ThirdParty/Mixamo/Idle.fbx",true);
        var walk=Clip("Assets/ThirdParty/Mixamo/Walking.fbx",true);
        var run=Clip(Blink+"Movement/RunForward.fbx",true);
        var attack=Clip(Blink+"Combat/MeleeAttack_OneHanded.fbx",false);
        // Some supplied action clips do not produce usable motion on this avatar. Reuse the verified swing.
        var attack2=attack;
        var dodge=Clip(Blink+"Movement/RollForward.fbx",false);
        var pulse=Clip(Blink+"Combat/Buff.fbx",false);
        var death=Clip(Blink+"Combat/Death.fbx",false);
        var hit=Clip(Blink+"Combat/GetHit.fbx",false);
        var windup=Clip(Blink+"Combat/IdleCombat.fbx",true);
        string path="Assets/Animation/RealmsGuardian.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(path))AssetDatabase.DeleteAsset(path);
        var controller=AnimatorController.CreateAnimatorControllerAtPath(path);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        var machine=controller.layers[0].stateMachine;
        var tree=new BlendTree{name="Mixamo idle + walk / Blink run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
        AssetDatabase.AddObjectToAsset(tree,controller);tree.AddChild(idle,0);tree.AddChild(walk,2);tree.AddChild(run,6.5f);
        var locomotion=machine.AddState("Locomotion");locomotion.motion=tree;machine.defaultState=locomotion;
        State(machine,"Idle",idle);State(machine,"Run",run);State(machine,"Attack",attack,.58f);State(machine,"Attack2",attack2,.58f);
        State(machine,"Dodge",dodge,.52f);State(machine,"Pulse",pulse,.65f);State(machine,"Death",death,1.15f);State(machine,"Hit",hit,.2f);State(machine,"Windup",windup);
        var player=UnityEngine.Object.Instantiate(game.guardianPrefab);player.GetComponentInChildren<Animator>().runtimeAnimatorController=controller;
        foreach(var c in player.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
        game.guardianPrefab=PrefabUtility.SaveAsPrefabAsset(player,"Assets/Prefabs/RealmsGuardian.prefab");
        var enemy=UnityEngine.Object.Instantiate(player);enemy.name="Corrupted knight / free Blink humanoid";
        foreach(var r in enemy.GetComponentsInChildren<Renderer>())
        {
            var mats=r.sharedMaterials;
            for(int i=0;i<mats.Length;i++)
            {
                string mp="Assets/Materials/StoreAdapted/Enemy_"+mats[i].name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
                if(!mat){mat=new Material(mats[i]);AssetDatabase.CreateAsset(mat,mp);}
                mat.SetColor("_BaseColor",new Color(.7f,.22f,.24f));mat.SetFloat("_Metallic",.55f);mats[i]=mat;
            }
            r.sharedMaterials=mats;
        }
        game.enemyHumanoidPrefab=PrefabUtility.SaveAsPrefabAsset(enemy,"Assets/Prefabs/CorruptedKnight.prefab");
        UnityEngine.Object.DestroyImmediate(player);UnityEngine.Object.DestroyImmediate(enemy);
        var oldBackground=GameObject.Find("Distant floating stones / Blender meshes");if(oldBackground)UnityEngine.Object.DestroyImmediate(oldBackground);
        var backdrop=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/RealmBackdrops.fbx"));backdrop.name="Realm horizon / Blender";
        var stone=Material("HorizonStone",new Color(.12f,.23f,.32f));
        foreach(var r in backdrop.GetComponentsInChildren<Renderer>())r.sharedMaterial=stone;
        // All solid scenery uses layer 8. Actor layers 9/10 collide through swept controllers.
        var floor=GameObject.Find("Walkable arena / radius 16.5m");floor.layer=8;
        Physics.IgnoreLayerCollision(9,10,false);Physics.IgnoreLayerCollision(10,10,false);
        game.levelRoots=new GameObject[3];game.levelSkies=new Material[3];
        var top=new[]{new Color(.055f,.22f,.45f),new Color(.16f,.055f,.15f),new Color(.025f,.035f,.12f)};
        var horizon=new[]{new Color(.52f,.73f,.8f),new Color(.86f,.36f,.16f),new Color(.23f,.28f,.51f)};
        for(int level=0;level<3;level++)
        {
            string skyPath="Assets/Materials/RealmSky"+level+".mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(!sky){sky=new Material(Shader.Find("Relic/RealmSky"));AssetDatabase.CreateAsset(sky,skyPath);}
            sky.SetColor("_Zenith",top[level]);sky.SetColor("_Horizon",horizon[level]);sky.SetColor("_Cloud",Color.Lerp(horizon[level],Color.white,.3f));sky.SetFloat("_Stars",level==2?1:0);
            game.levelSkies[level]=sky;
            var root=new GameObject(new[]{"Level 1 / Azure Sanctuary","Level 2 / Ember Citadel","Level 3 / Astral Summit"}[level]);game.levelRoots[level]=root;
            var accent=new[]{new Color(.10f,.8f,.8f),new Color(1,.26f,.055f),new Color(.48f,.32f,1)}[level];
            var glowing=Material("RealmGlow"+level,accent,true);var block=Material("RealmStone"+level,Color.Lerp(top[level],Color.gray,.35f));
            for(int i=0;i<4;i++)
            {
                float angle=(45+i*90+(level==1?20:0))*Mathf.Deg2Rad;
                var p=new Vector3(Mathf.Sin(angle)*(level==2?10:8),0,Mathf.Cos(angle)*(level==2?10:8));
                var pedestal=Primitive(root,"Solid cover",PrimitiveType.Cylinder,p+Vector3.up*.8f,new Vector3(2.1f,.8f,2.1f),block,true);
                var gem=Primitive(root,"Realm crystal",PrimitiveType.Cube,p+Vector3.up*2.15f,new Vector3(.7f,1.6f,.7f),glowing,false);gem.transform.rotation=Quaternion.Euler(15,i*47,15);
                var light=gem.AddComponent<Light>();light.type=LightType.Point;light.color=accent;light.intensity=2;light.range=6;light.shadows=LightShadows.None;
            }
            if(level==1)
            {
                Primitive(root,"Citadel east barricade",PrimitiveType.Cube,new Vector3(11,1,0),new Vector3(1.5f,2,4),block,true);
                Primitive(root,"Citadel west barricade",PrimitiveType.Cube,new Vector3(-11,1,0),new Vector3(1.5f,2,4),block,true);
            }
            // Decorative geometry stays outside the playable boundary.
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;var p=new Vector3(Mathf.Sin(a)*23,level==2?4:-1,Mathf.Cos(a)*23);
                var shard=Primitive(root,"Horizon shard",PrimitiveType.Cube,p,new Vector3(1,level==2?5:2,1),glowing,false);shard.transform.rotation=Quaternion.Euler(20,i*31,25);
            }
            root.SetActive(level==0);
        }
        // Combine the many fixed temple pieces at build time to reduce render submission cost.
        foreach(var root in new[]{environment,backdrop}.Concat(game.levelRoots))
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);
        game.ApplyLevel(0);EditorUtility.SetDirty(game);
        Debug.Log("REALMS_COMPLETE: 3 levels, Mixamo humanoid blend, solid cover and Blender backgrounds");
    }
    static Material Material(string name,Color color,bool glow=false)
    {
        string path="Assets/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.45f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2);}
        return m;
    }
    static GameObject Primitive(GameObject root,string name,PrimitiveType type,Vector3 p,Vector3 scale,Material material,bool solid)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.parent=root.transform;o.transform.position=p;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;
        if(solid)
        {
            o.layer=8;
            if(type==PrimitiveType.Cylinder)
            {
                UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
                o.AddComponent<MeshCollider>().sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;
            }
        }
        else UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        return o;
    }
}
