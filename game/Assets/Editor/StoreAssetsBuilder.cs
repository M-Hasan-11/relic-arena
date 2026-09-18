using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using RelicArena;

public static class StoreAssetsBuilder
{
    const string CharacterPath="Assets/Blink/Art/Characters/LowPoly/FREE_HumanLowPoly/Prefabs_Humans/HumanMale_Character_FREE.prefab";
    const string AnimationRoot="Assets/Blink/Art/Animations/Animations_Starter_Pack/";
    public static void UpgradeScene(RelicGame game, GameObject environment)
    {
        Directory.CreateDirectory("Assets/Materials/StoreAdapted");Directory.CreateDirectory("Assets/Animation");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        if(!source)throw new Exception("Blink free character has not been imported.");
        var player=UnityEngine.Object.Instantiate(source);player.name="Guardian / Blink free character";
        foreach(var t in player.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        foreach(var r in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            bool armor=r.name.StartsWith("PlateSet1_");
            bool head=r.name=="Head"||r.name=="Neck"||r.name=="Eyes"||r.name=="Eyebrows"||r.name=="Eyelashes";
            r.gameObject.SetActive(armor||head);
            var list=r.sharedMaterials;
            for(int i=0;i<list.Length;i++)if(list[i])list[i]=ConvertMaterial(list[i]);
            r.sharedMaterials=list;r.updateWhenOffscreen=true;
        }
        var animator=player.GetComponentInChildren<Animator>();
        if(!animator || !animator.avatar || !animator.avatar.isHuman)throw new Exception("Blink humanoid avatar is invalid.");
        animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController=CreateController();animator.Rebind();
        // Reuse the Blender sword at its original world dimensions on the humanoid hand.
        var old=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Guardian.prefab");
        var weapon=old.GetComponentsInChildren<Transform>().First(t=>t.name=="Weapon");
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        var sword=UnityEngine.Object.Instantiate(weapon.gameObject,hand);sword.name="Blender relic sword";
        sword.transform.localPosition=new Vector3(.01f,.025f,0);
        sword.transform.localRotation=Quaternion.Euler(0,0,180);
        sword.transform.localScale=Vector3.one*(Mathf.Abs(weapon.lossyScale.x)/Mathf.Abs(hand.lossyScale.x));
        var prefab=PrefabUtility.SaveAsPrefabAsset(player,"Assets/Prefabs/GuardianV2.prefab");game.guardianPrefab=prefab;
        UnityEngine.Object.DestroyImmediate(player);
        game.particleMaterial=ParticleMaterial("RelicSpark","RoundSoftParticle.tif",true);
        game.mistMaterial=ParticleMaterial("TempleMist","smokeysteam.tif",false);
        CreateAtmosphere(game,environment);
        Debug.Log("RELIC_STORE_ASSETS_COMPLETE: Blink humanoid + original animations; Unity Particle Pack textures");
    }
    static Material ConvertMaterial(Material original)
    {
        string path="Assets/Materials/StoreAdapted/Blink_"+original.name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        var color=original.HasProperty("_Color")?original.GetColor("_Color"):Color.white;
        m.SetColor("_BaseColor",color);m.SetTexture("_BaseMap",original.mainTexture);
        m.SetTextureScale("_BaseMap",original.mainTextureScale);m.SetTextureOffset("_BaseMap",original.mainTextureOffset);
        m.SetFloat("_Metallic",original.name.Contains("Armor")?.45f:.08f);m.SetFloat("_Smoothness",.4f);
        EditorUtility.SetDirty(m);return m;
    }
    static AnimatorController CreateController()
    {
        string path="Assets/Animation/Guardian.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(controller)return controller;
        controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine=controller.layers[0].stateMachine;
        AddState(machine,"Idle","Combat/IdleCombat.fbx",true,1);
        AddState(machine,"Run","Movement/RunForward.fbx",true,1.15f);
        AddState(machine,"Attack","Combat/MeleeAttack_OneHanded.fbx",false,2.2f);
        AddState(machine,"Dodge","Movement/RollForward.fbx",false,2.5f);
        AddState(machine,"Pulse","Combat/Buff.fbx",false,1.7f);
        AddState(machine,"Death","Combat/Death.fbx",false,1);
        return controller;
    }
    static void AddState(AnimatorStateMachine machine,string name,string relative,bool loop,float speed)
    {
        string path=AnimationRoot+relative;
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        var clips=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
        foreach(var clip in clips){clip.loopTime=loop;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.lockRootRotation=true;}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var animation=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
        if(!animation)throw new Exception("Missing animation: "+path);
        var state=machine.AddState(name);state.motion=animation;state.speed=speed;
        if(name=="Idle")machine.defaultState=state;
    }
    static Material ParticleMaterial(string name,string textureName,bool additive)
    {
        string path="Assets/Materials/StoreAdapted/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(m,path);}
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/UnityParticlePack/Textures/"+textureName);
        if(!texture)throw new Exception("Missing Unity Particle Pack texture: "+textureName);
        m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",Color.white);
        m.SetFloat("_Surface",1);m.SetFloat("_Blend",additive?2:0);m.SetFloat("_ZWrite",0);
        m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);return m;
    }
    static void CreateAtmosphere(RelicGame game,GameObject environment)
    {
        var mist=new GameObject("Cloud sea / Unity Particle Pack");mist.transform.position=new Vector3(0,-24,0);
        var ps=mist.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.prewarm=true;main.duration=40;main.startLifetime=40;main.startSpeed=0;
        main.startSize=new ParticleSystem.MinMaxCurve(20,30);main.startColor=new Color(.38f,.54f,.66f,.12f);main.maxParticles=70;
        main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.rateOverTime=1.5f;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(115,2,115);
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=game.mistMaterial;
        var motes=new GameObject("Relic motes / Unity Particle Pack");motes.transform.position=Vector3.up*1.5f;
        var particles=motes.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        main=particles.main;main.loop=true;main.prewarm=true;main.duration=12;main.startLifetime=12;main.startSpeed=.15f;
        main.startSize=new ParticleSystem.MinMaxCurve(.035f,.075f);main.startColor=new Color(.24f,.92f,.75f,.6f);main.maxParticles=80;
        emission=particles.emission;emission.rateOverTime=5;
        shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(30,3,30);
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=game.particleMaterial;
        var root=new GameObject("Distant floating stones / Blender meshes");
        var stone=environment.GetComponentsInChildren<MeshFilter>().First(m=>m.name.StartsWith("Understone"));
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI*2/12;var rock=new GameObject("Floating isle "+i);rock.transform.parent=root.transform;
            rock.transform.position=new Vector3(Mathf.Sin(a)*62,-9+(i%3)*5,Mathf.Cos(a)*62);
            rock.transform.rotation=Quaternion.Euler(0,i*71,0);rock.transform.localScale=stone.transform.lossyScale*(2.3f+(i%3));
            rock.AddComponent<MeshFilter>().sharedMesh=stone.sharedMesh;rock.AddComponent<MeshRenderer>().sharedMaterials=stone.GetComponent<MeshRenderer>().sharedMaterials;
        }
    }
}
